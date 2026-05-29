using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using ColumnMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata;
using ConstraintMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ConstraintMetadata;
using ForeignKeyConstraintModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ForeignKeyConstraint;
using FunctionMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.FunctionMetadata;
using IndexMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.IndexMetadata;
using PrimaryKeyConstraintModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.PrimaryKeyConstraint;
using StoredProcedureMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.StoredProcedureMetadata;
using TriggerMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.TriggerMetadata;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider, ISchemaDiscoveryProvider, IForeignKeyDiscoveryProvider, IColumnDiscoveryProvider, IIndexDiscoveryProvider, IPrimaryKeyDiscoveryProvider, ITableDiscoveryProvider, IViewDiscoveryProvider, IStoredProcedureDiscoveryProvider, IFunctionDiscoveryProvider, ITriggerDiscoveryProvider, IConstraintDiscoveryProvider, IObjectDefinitionProvider
{
    private const string DiscoverSchemasSql = """
        SELECT schema_id, name
        FROM sys.schemas
        WHERE schema_id > 0
          AND (
              @IncludeSystemSchemas = 1
              OR name NOT IN (N'dbo', N'guest', N'INFORMATION_SCHEMA', N'sys')
          )
        ORDER BY name;
        """;

    private const string DiscoverTablesSql = """
        SELECT
            t.object_id,
            SCHEMA_NAME(t.schema_id) AS schema_name,
            t.name AS table_name,
            ISNULL(SUM(ps.row_count), 0) AS row_count
        FROM sys.tables AS t
        LEFT JOIN sys.dm_db_partition_stats AS ps ON t.object_id = ps.object_id
            AND ps.index_id IN (0, 1)
        WHERE (@IncludeSystemTables = 1 OR t.is_ms_shipped = 0)
          AND (@SchemaName IS NULL OR SCHEMA_NAME(t.schema_id) = @SchemaName)
        GROUP BY t.object_id, SCHEMA_NAME(t.schema_id), t.name
        ORDER BY schema_name, table_name;
        """;

    private const string DiscoverViewsSql = """
        SELECT
            v.object_id,
            SCHEMA_NAME(v.schema_id) AS schema_name,
            v.name AS view_name,
            CASE WHEN OBJECT_DEFINITION(v.object_id) IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS has_definition
        FROM sys.views AS v
        WHERE (@IncludeSystemViews = 1 OR v.is_ms_shipped = 0)
          AND (@SchemaName IS NULL OR SCHEMA_NAME(v.schema_id) = @SchemaName)
        ORDER BY schema_name, view_name;
        """;

    private const string DiscoverStoredProceduresSql = """
        SELECT
            p.object_id,
            SCHEMA_NAME(p.schema_id) AS schema_name,
            p.name AS procedure_name,
            CASE WHEN OBJECT_DEFINITION(p.object_id) IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS has_definition,
            p.create_date,
            prm.parameter_id,
            prm.name AS parameter_name,
            typ.name AS parameter_type,
            prm.max_length,
            prm.precision,
            prm.scale,
            prm.is_output,
            sm.definition
        FROM sys.procedures AS p
        LEFT JOIN sys.parameters AS prm ON p.object_id = prm.object_id
        LEFT JOIN sys.types AS typ ON prm.user_type_id = typ.user_type_id
        LEFT JOIN sys.sql_modules AS sm ON p.object_id = sm.object_id
        WHERE (@IncludeSystemProcedures = 1 OR p.is_ms_shipped = 0)
          AND (@SchemaName IS NULL OR SCHEMA_NAME(p.schema_id) = @SchemaName)
        ORDER BY schema_name, procedure_name, prm.parameter_id;
        """;

    private const string DiscoverFunctionsSql = """
        SELECT
            o.object_id,
            SCHEMA_NAME(o.schema_id) AS schema_name,
            o.name AS function_name,
            o.type AS function_type_code,
            return_type.name AS return_type_name,
            return_param.max_length,
            return_param.precision,
            return_param.scale,
            CASE WHEN OBJECT_DEFINITION(o.object_id) IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS has_definition,
            o.create_date,
            prm.parameter_id,
            prm.name AS parameter_name,
            param_type.name AS parameter_type,
            prm.max_length,
            prm.precision,
            prm.scale,
            sm.definition
        FROM sys.objects AS o
        INNER JOIN sys.functions AS f ON o.object_id = f.object_id
        LEFT JOIN sys.parameters AS return_param ON o.object_id = return_param.object_id AND return_param.parameter_id = 0
        LEFT JOIN sys.types AS return_type ON return_param.user_type_id = return_type.user_type_id
        LEFT JOIN sys.parameters AS prm ON o.object_id = prm.object_id AND prm.parameter_id > 0
        LEFT JOIN sys.types AS param_type ON prm.user_type_id = param_type.user_type_id
        LEFT JOIN sys.sql_modules AS sm ON o.object_id = sm.object_id
        WHERE o.type IN (N'FN', N'TF', N'IF')
          AND (@IncludeSystemFunctions = 1 OR o.is_ms_shipped = 0)
        ORDER BY schema_name, function_name;
        """;

    private const string DiscoverTriggersSql = """
        SELECT
            t.object_id,
            t.name AS trigger_name,
            SCHEMA_NAME(t.schema_id) AS schema_name,
            SCHEMA_NAME(parent.schema_id) AS parent_schema_name,
            COALESCE(parent.name, DB_NAME()) AS parent_object_name,
            t.parent_class,
            t.is_disabled,
            t.is_instead_of_trigger,
            CASE WHEN OBJECT_DEFINITION(t.object_id) IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS has_definition,
            trigger_object.create_date,
            te.type_desc AS trigger_event_type
        FROM sys.triggers AS t
        LEFT JOIN sys.objects AS parent ON t.parent_id = parent.object_id
        LEFT JOIN sys.objects AS trigger_object ON t.object_id = trigger_object.object_id
        LEFT JOIN sys.trigger_events AS te ON t.object_id = te.object_id
        WHERE (@SchemaName IS NULL OR SCHEMA_NAME(t.schema_id) = @SchemaName)
          AND (@ParentObjectName IS NULL OR COALESCE(parent.name, DB_NAME()) = @ParentObjectName)
        ORDER BY schema_name, parent_object_name, trigger_name, trigger_event_type;
        """;

    private const string DiscoverForeignKeysSql = """
        SELECT
            fk.object_id,
            fk.name AS constraint_name,
            ps.name AS parent_schema,
            pt.name AS parent_table,
            rs.name AS referenced_schema,
            rt.name AS referenced_table,
            pc.name AS parent_column,
            rc.name AS referenced_column,
            fkc.constraint_column_id,
            fk.delete_referential_action,
            fk.update_referential_action,
            fk.is_disabled
        FROM sys.foreign_keys AS fk
        INNER JOIN sys.tables AS pt ON fk.parent_object_id = pt.object_id
        INNER JOIN sys.schemas AS ps ON pt.schema_id = ps.schema_id
        INNER JOIN sys.tables AS rt ON fk.referenced_object_id = rt.object_id
        INNER JOIN sys.schemas AS rs ON rt.schema_id = rs.schema_id
        INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.columns AS pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
        INNER JOIN sys.columns AS rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
        WHERE (@ParentSchemaName IS NULL OR ps.name = @ParentSchemaName)
          AND (@ParentTableName IS NULL OR pt.name = @ParentTableName)
        ORDER BY fk.object_id, fkc.constraint_column_id;
        """;

    private const string DiscoverColumnsSql = """
        SELECT
            c.object_id,
            c.column_id,
            c.name AS column_name,
            t.name AS data_type,
            c.max_length,
            c.precision,
            c.scale,
            c.is_nullable,
            CAST(ISNULL(ic.is_identity, 0) AS bit) AS is_identity,
            CAST(ISNULL(cc.is_computed, 0) AS bit) AS is_computed,
            dc.definition AS default_value,
            CAST(ep.value AS nvarchar(4000)) AS description
        FROM sys.columns AS c
        INNER JOIN sys.objects AS o ON c.object_id = o.object_id
        INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id
        INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
        LEFT JOIN sys.identity_columns AS ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        LEFT JOIN sys.computed_columns AS cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
        LEFT JOIN sys.default_constraints AS dc ON c.default_object_id = dc.object_id
        LEFT JOIN sys.extended_properties AS ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.class = 1 AND ep.name = N'MS_Description'
        WHERE (
                @ObjectId IS NOT NULL AND c.object_id = @ObjectId
            )
            OR (
                @ObjectId IS NULL
                AND s.name = @SchemaName
                AND o.name = @ObjectName
                AND o.type = @ObjectType
            )
        ORDER BY c.column_id;
        """;

    private const string DiscoverIndexesSql = """
        SELECT
            i.object_id,
            i.index_id,
            i.name AS index_name,
            s.name AS schema_name,
            t.name AS table_name,
            i.is_primary_key,
            i.is_unique,
            CAST(CASE WHEN i.type IN (1, 5) THEN 1 ELSE 0 END AS bit) AS is_clustered,
            c.name AS column_name,
            ic.is_included_column,
            ic.key_ordinal,
            ic.index_column_id,
            i.filter_definition
        FROM sys.indexes AS i
        INNER JOIN sys.tables AS t ON i.object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS c ON t.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE i.index_id > 0
          AND i.is_hypothetical = 0
          AND (@SchemaName IS NULL OR s.name = @SchemaName)
          AND (@TableName IS NULL OR t.name = @TableName)
          AND (ic.key_ordinal > 0 OR ic.is_included_column = 1)
        ORDER BY s.name, t.name, i.name, ic.is_included_column, CASE
            WHEN ic.is_included_column = 1 THEN ic.index_column_id
            ELSE ic.key_ordinal
        END;
        """;

    private const string DiscoverPrimaryKeysSql = """
        SELECT
            kc.object_id,
            kc.name AS constraint_name,
            s.name AS schema_name,
            t.name AS table_name,
            CAST(CASE WHEN i.type IN (1, 5) THEN 1 ELSE 0 END AS bit) AS is_clustered,
            c.name AS column_name,
            ic.key_ordinal
        FROM sys.key_constraints AS kc
        INNER JOIN sys.tables AS t ON kc.parent_object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.indexes AS i ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
        INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS c ON t.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE kc.type = 'PK'
          AND (@SchemaName IS NULL OR s.name = @SchemaName)
          AND (@TableName IS NULL OR t.name = @TableName)
          AND ic.key_ordinal > 0
        ORDER BY s.name, t.name, kc.name, ic.key_ordinal;
        """;

    private const string DiscoverConstraintsSql = """
        SELECT
            dc.object_id,
            dc.name AS constraint_name,
            s.name AS schema_name,
            t.name AS table_name,
            c.name AS column_name,
            dc.definition,
            CAST(0 AS bit) AS is_disabled,
            N'D' AS constraint_type
        FROM sys.default_constraints AS dc
        INNER JOIN sys.tables AS t ON dc.parent_object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.columns AS c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE (@SchemaName IS NULL OR s.name = @SchemaName)
          AND (@TableName IS NULL OR t.name = @TableName)

        UNION ALL

        SELECT
            cc.object_id,
            cc.name AS constraint_name,
            s.name AS schema_name,
            t.name AS table_name,
            c.name AS column_name,
            cc.definition,
            cc.is_disabled,
            N'C' AS constraint_type
        FROM sys.check_constraints AS cc
        INNER JOIN sys.tables AS t ON cc.parent_object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        LEFT JOIN sys.columns AS c ON cc.parent_object_id = c.object_id AND cc.parent_column_id > 0 AND cc.parent_column_id = c.column_id
        WHERE (@SchemaName IS NULL OR s.name = @SchemaName)
          AND (@TableName IS NULL OR t.name = @TableName)

        UNION ALL

        SELECT
            kc.object_id,
            kc.name AS constraint_name,
            s.name AS schema_name,
            t.name AS table_name,
            NULL AS column_name,
            NULL AS definition,
            CAST(0 AS bit) AS is_disabled,
            N'U' AS constraint_type
        FROM sys.key_constraints AS kc
        INNER JOIN sys.tables AS t ON kc.parent_object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        WHERE kc.type = N'UQ'
          AND (@SchemaName IS NULL OR s.name = @SchemaName)
          AND (@TableName IS NULL OR t.name = @TableName)

        ORDER BY schema_name, table_name, constraint_type, constraint_name;
        """;

    private const string GetObjectDefinitionSql = """
        SELECT OBJECT_DEFINITION(@ObjectId) AS definition;
        """;

    private const string GetIndexDefinitionSql = """
        SELECT
            i.name AS index_name,
            s.name AS schema_name,
            t.name AS table_name,
            i.is_unique,
            CAST(CASE WHEN i.type IN (1, 5) THEN 1 ELSE 0 END AS bit) AS is_clustered,
            i.is_primary_key,
            c.name AS column_name,
            ic.is_included_column,
            ic.key_ordinal,
            ic.index_column_id,
            i.filter_definition
        FROM sys.indexes AS i
        INNER JOIN sys.tables AS t ON i.object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS c ON t.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE i.object_id = @ObjectId
          AND i.index_id = @IndexId
          AND (ic.key_ordinal > 0 OR ic.is_included_column = 1)
        ORDER BY ic.is_included_column, CASE
            WHEN ic.is_included_column = 1 THEN ic.index_column_id
            ELSE ic.key_ordinal
        END;
        """;

    public string ProviderName => "sqlserver";
    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    public ProviderCapabilities Capabilities { get; } = new()
    {
        SupportsSchemas = true,
        SupportsTables = true,
        SupportsViews = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true,
        SupportsTriggers = true,
        SupportsIndexes = true,
        SupportsConstraints = true,
        SupportsKeys = true,
        SupportsDefinitionRetrieval = true,
        SupportsLiveStats = false,
    };

    public bool CanHandle(DatabaseResource resource)
        => resource.Provider.Contains("sqlserver", StringComparison.OrdinalIgnoreCase)
            || resource.Provider.Contains("mssql", StringComparison.OrdinalIgnoreCase)
            || resource.Provider.Contains("sqlclient", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(
        DatabaseResource resource,
        CancellationToken cancellationToken)
    {
        var response = await DiscoverSchemasAsync(resource, new DiscoverSchemasRequest(), cancellationToken);

        return response.Schemas
            .Select(schema => new SchemaMetadata(
                schema.ObjectName,
                Tables: Array.Empty<TableMetadata>(),
                Views: Array.Empty<ViewMetadata>()))
            .ToList();
    }

    public async Task<DiscoverSchemasResponse> DiscoverSchemasAsync(
        DatabaseResource resource,
        DiscoverSchemasRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverSchemasCommand(connection, request.IncludeSystemSchemas);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var discovered = new List<SchemaObject>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var schemaId = reader.GetInt32(0);
                var schemaName = reader.GetString(1);
                discovered.Add(CreateSchemaObject(schemaId, schemaName));
            }

            return new DiscoverSchemasResponse(discovered);
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverSchemasResponse(Array.Empty<SchemaObject>());
        }
    }

    public async Task<DiscoverTablesResponse> DiscoverTablesAsync(
        DatabaseResource resource,
        DiscoverTablesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverTablesCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<TableDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new TableDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    SchemaName: reader.GetString(1),
                    TableName: reader.GetString(2),
                    RowCount: reader.GetInt64(3)));
            }

            return new DiscoverTablesResponse(NormalizeTables(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverTablesResponse(Array.Empty<TableObject>());
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(
        DatabaseResource resource,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            throw new ArgumentException("Query text is required.", nameof(request));
        }

        if (request.MaxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "MaxRows must be greater than zero.");
        }

        await using var connection = new SqlConnection(resource.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = request.IncludeExecutionPlan
            ? BuildStatisticsXmlCommandText(request.Sql)
            : request.Sql;
        command.CommandTimeout = ResolveCommandTimeoutSeconds(request);

        var stopwatch = Stopwatch.StartNew();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<string>();
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        var maxRows = request.MaxRows;
        var isTruncated = false;
        string? executionPlanXml = null;

        do
        {
            if (reader.FieldCount <= 0)
            {
                continue;
            }

            if (request.IncludeExecutionPlan && IsExecutionPlanResultSet(reader))
            {
                if (await reader.ReadAsync(cancellationToken) && !reader.IsDBNull(0))
                {
                    executionPlanXml = Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture);
                }

                continue;
            }

            if (columns.Count == 0)
            {
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    columns.Add(reader.GetName(index));
                }
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count >= maxRows)
                {
                    isTruncated = true;
                    break;
                }

                var row = new Dictionary<string, object?>(columns.Count, StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < columns.Count; index++)
                {
                    row[columns[index]] = reader.IsDBNull(index)
                        ? null
                        : reader.GetValue(index);
                }

                rows.Add(row);
            }

            if (isTruncated)
            {
                break;
            }
        }
        while (await reader.NextResultAsync(cancellationToken));

        var elapsed = stopwatch.Elapsed;
        int? affectedRows = reader.RecordsAffected >= 0
            ? reader.RecordsAffected
            : null;

        var executionPlan = request.IncludeExecutionPlan
            ? BuildExecutionPlanResult(executionPlanXml)
            : null;

        return new QueryResult(
            Columns: columns,
            Rows: rows,
            RowCount: rows.Count,
            Duration: elapsed,
            AffectedRowCount: affectedRows,
            IsTruncated: isTruncated,
            ExecutionPlan: executionPlan);
    }

    internal static bool IsExecutionPlanResultSet(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.FieldCount != 1)
        {
            return false;
        }

        var columnName = reader.GetName(0);
        return columnName.Contains("Showplan", StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildStatisticsXmlCommandText(string sql)
        => $"SET STATISTICS XML ON;{Environment.NewLine}{sql}{Environment.NewLine}SET STATISTICS XML OFF;";

    internal static QueryExecutionPlanResult BuildExecutionPlanResult(string? executionPlanXml)
    {
        if (string.IsNullOrWhiteSpace(executionPlanXml))
        {
            return new QueryExecutionPlanResult(
                IsAvailable: false,
                Provider: "SqlServer",
                MermaidDiagram: null,
                RawPlan: null,
                Message: "Execution plan is not available for this query or provider.");
        }

        try
        {
            return new QueryExecutionPlanResult(
                IsAvailable: true,
                Provider: "SqlServer",
                MermaidDiagram: ConvertExecutionPlanXmlToMermaid(executionPlanXml),
                RawPlan: executionPlanXml,
                Message: null);
        }
        catch
        {
            return new QueryExecutionPlanResult(
                IsAvailable: false,
                Provider: "SqlServer",
                MermaidDiagram: null,
                RawPlan: executionPlanXml,
                Message: "Execution plan is not available for this query or provider.");
        }
    }

    internal static string ConvertExecutionPlanXmlToMermaid(string executionPlanXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionPlanXml);

        var document = XDocument.Parse(executionPlanXml);
        XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var relOps = document
            .Descendants(ns + "RelOp")
            .Take(32)
            .ToList();

        if (relOps.Count == 0)
        {
            throw new InvalidOperationException("Execution plan does not include RelOp operators.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("flowchart LR");
        builder.AppendLine("    classDef epOperator fill:#0f172a,stroke:#60a5fa,stroke-width:1px,color:#e2e8f0;");
        builder.AppendLine("    classDef epAccess fill:#0b1b33,stroke:#38bdf8,stroke-width:1px,color:#e0f2fe;");
        builder.AppendLine("    classDef epJoin fill:#2a1736,stroke:#c084fc,stroke-width:1px,color:#f5e8ff;");
        builder.AppendLine("    classDef epCompute fill:#1f2937,stroke:#34d399,stroke-width:1px,color:#ecfeff;");

        var nodeIds = new Dictionary<XElement, string>();
        var usedNodeIds = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < relOps.Count; index++)
        {
            var relOp = relOps[index];
            var nodeId = BuildMermaidNodeId(relOp, index, usedNodeIds);
            nodeIds[relOp] = nodeId;
            builder.AppendLine($"    {nodeId}[\"{EscapeMermaidLabel(BuildExecutionPlanNodeLabel(relOp, ns))}\"]");
            builder.AppendLine($"    class {nodeId} {ResolveExecutionPlanNodeClass(relOp)}");
        }

        var hasEdges = false;
        foreach (var relOp in relOps)
        {
            var parentNodeId = nodeIds[relOp];
            var childRelOps = relOp
                .Descendants(ns + "RelOp")
                .Where(child => !ReferenceEquals(child, relOp) && ReferenceEquals(child.Ancestors(ns + "RelOp").FirstOrDefault(), relOp));

            foreach (var childRelOp in childRelOps)
            {
                if (!nodeIds.TryGetValue(childRelOp, out var childNodeId))
                {
                    continue;
                }

                builder.AppendLine($"    {parentNodeId} --> {childNodeId}");
                hasEdges = true;
            }
        }

        if (!hasEdges)
        {
            for (var index = 1; index < relOps.Count; index++)
            {
                builder.AppendLine($"    {nodeIds[relOps[index - 1]]} --> {nodeIds[relOps[index]]}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildExecutionPlanNodeLabel(XElement relOp, XNamespace ns)
    {
        var lines = new List<string>();
        const string metricIndent = "&nbsp;&nbsp;";
        var physicalOp = relOp.Attribute("PhysicalOp")?.Value;
        var logicalOp = relOp.Attribute("LogicalOp")?.Value;

        lines.Add(!string.IsNullOrWhiteSpace(physicalOp)
            ? physicalOp!
            : logicalOp ?? "Operation");

        var objectName = TryBuildObjectName(relOp, ns);
        if (!string.IsNullOrWhiteSpace(objectName))
        {
            lines.Add($"Object: {objectName}");
        }

        if (!string.IsNullOrWhiteSpace(logicalOp)
            && !string.Equals(logicalOp, physicalOp, StringComparison.OrdinalIgnoreCase))
        {
            lines.Add($"Logical: {logicalOp}");
        }

        AddAttributePart(lines, relOp, "EstimateRows", $"{metricIndent}Estimated Rows");
        AddAttributePart(lines, relOp, "EstimatedTotalSubtreeCost", $"{metricIndent}Estimated Cost");
        AddAttributePart(lines, relOp, "EstimateIO", $"{metricIndent}Estimated I/O");
        AddAttributePart(lines, relOp, "EstimateCPU", $"{metricIndent}Estimated CPU");

        var runtimeCounters = relOp
            .Descendants(ns + "RunTimeCountersPerThread")
            .Take(64)
            .ToList();

        AddRuntimeCounterPart(lines, runtimeCounters, "ActualRows", $"{metricIndent}Actual Rows");
        AddRuntimeCounterPart(lines, runtimeCounters, "ActualExecutions", $"{metricIndent}Actual Execs");
        AddRuntimeCounterPart(lines, runtimeCounters, "ActualElapsedms", $"{metricIndent}Actual Elapsed ms");
        AddRuntimeCounterPart(lines, runtimeCounters, "ActualCPUms", $"{metricIndent}Actual CPU ms");
        AddRuntimeCounterPart(lines, runtimeCounters, "ActualLogicalReads", $"{metricIndent}Actual Reads");

        return JoinExecutionPlanLabelLines(lines);
    }

    private static string JoinExecutionPlanLabelLines(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            return string.Empty;
        }

        if (lines.Count == 1)
        {
            return lines[0];
        }

        return string.Join("<br/>--------<br/>", lines);
    }

    private static string BuildMermaidNodeId(XElement relOp, int index, ISet<string> usedNodeIds)
    {
        ArgumentNullException.ThrowIfNull(relOp);
        ArgumentNullException.ThrowIfNull(usedNodeIds);

        var rawNodeId = relOp.Attribute("NodeId")?.Value;
        var seed = int.TryParse(rawNodeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedNodeId)
            ? parsedNodeId
            : index + 1;

        var candidate = $"N{seed}";
        var duplicateIndex = 2;
        while (!usedNodeIds.Add(candidate))
        {
            candidate = $"N{seed}_{duplicateIndex++}";
        }

        return candidate;
    }

    private static void AddAttributePart(ICollection<string> parts, XElement relOp, string attributeName, string label)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(relOp);

        var value = relOp.Attribute(attributeName)?.Value;
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value}");
        }
    }

    private static void AddRuntimeCounterPart(ICollection<string> parts, IReadOnlyCollection<XElement> runtimeCounters, string attributeName, string label)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(runtimeCounters);

        var value = AggregateRuntimeCounter(runtimeCounters, attributeName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value}");
        }
    }

    private static string ResolveExecutionPlanNodeClass(XElement relOp)
    {
        ArgumentNullException.ThrowIfNull(relOp);

        var operation = relOp.Attribute("PhysicalOp")?.Value
            ?? relOp.Attribute("LogicalOp")?.Value
            ?? string.Empty;

        if (operation.Contains("Join", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("Apply", StringComparison.OrdinalIgnoreCase))
        {
            return "epJoin";
        }

        if (operation.Contains("Scan", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("Seek", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("Lookup", StringComparison.OrdinalIgnoreCase))
        {
            return "epAccess";
        }

        if (operation.Contains("Sort", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("Aggregate", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("Compute", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("Filter", StringComparison.OrdinalIgnoreCase))
        {
            return "epCompute";
        }

        return "epOperator";
    }

    private static string? AggregateRuntimeCounter(IReadOnlyCollection<XElement> runtimeCounters, string attributeName)
    {
        if (runtimeCounters.Count == 0)
        {
            return null;
        }

        var values = runtimeCounters
            .Select(counter => counter.Attribute(attributeName)?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();

        if (values.Count == 0)
        {
            return null;
        }

        var sum = 0d;
        foreach (var value in values)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return value;
            }

            sum += parsed;
        }

        return sum.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string? TryBuildObjectName(XElement relOp, XNamespace ns)
    {
        var objectElement = relOp.Descendants(ns + "Object").FirstOrDefault();
        if (objectElement is null)
        {
            return null;
        }

        var schema = SanitizeSqlIdentifier(objectElement.Attribute("Schema")?.Value);
        var table = SanitizeSqlIdentifier(objectElement.Attribute("Table")?.Value);
        var index = SanitizeSqlIdentifier(objectElement.Attribute("Index")?.Value);

        var objectName = !string.IsNullOrWhiteSpace(schema) && !string.IsNullOrWhiteSpace(table)
            ? $"{schema}.{table}"
            : table ?? schema;

        if (string.IsNullOrWhiteSpace(objectName))
        {
            objectName = SanitizeSqlIdentifier(objectElement.Attribute("Alias")?.Value);
        }

        return string.IsNullOrWhiteSpace(index)
            ? objectName
            : $"{objectName} ({index})";
    }

    private static string? SanitizeSqlIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Trim()
            .Trim('[', ']');
    }

    internal static string EscapeMermaidLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Operation";
        }

        return text
            .Replace("\"", "'", StringComparison.Ordinal)
            .Replace("[", "(", StringComparison.Ordinal)
            .Replace("]", ")", StringComparison.Ordinal)
            .Replace("{", "(", StringComparison.Ordinal)
            .Replace("}", ")", StringComparison.Ordinal);
    }

    internal static int ResolveCommandTimeoutSeconds(ExecuteQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.TimeoutSeconds.GetValueOrDefault() > 0
            ? request.TimeoutSeconds!.Value
            : 30;
    }

    public async Task<DiscoverForeignKeysResponse> DiscoverForeignKeysAsync(
        DatabaseResource resource,
        DiscoverForeignKeysRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverForeignKeysCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<ForeignKeyDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new ForeignKeyDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    ConstraintName: reader.GetString(1),
                    ParentSchemaName: reader.GetString(2),
                    ParentTableName: reader.GetString(3),
                    ReferencedSchemaName: reader.GetString(4),
                    ReferencedTableName: reader.GetString(5),
                    ParentColumnName: reader.GetString(6),
                    ReferencedColumnName: reader.GetString(7),
                    ConstraintColumnId: Convert.ToInt32(reader.GetValue(8), CultureInfo.InvariantCulture),
                    DeleteReferentialAction: Convert.ToInt32(reader.GetValue(9), CultureInfo.InvariantCulture),
                    UpdateReferentialAction: Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
                    IsDisabled: reader.GetBoolean(11)));
            }

            return new DiscoverForeignKeysResponse(NormalizeForeignKeyConstraints(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverForeignKeysResponse(Array.Empty<ForeignKeyConstraintModel>());
        }
    }

    public async Task<DiscoverColumnsResponse> DiscoverColumnsAsync(
        DatabaseResource resource,
        DiscoverColumnsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverColumnsCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<ColumnDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new ColumnDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    ColumnId: reader.GetInt32(1),
                    Name: reader.GetString(2),
                    DataType: reader.GetString(3),
                    MaxLength: reader.IsDBNull(4) ? null : reader.GetInt16(4),
                    Precision: reader.IsDBNull(5) ? null : reader.GetByte(5),
                    Scale: reader.IsDBNull(6) ? null : reader.GetByte(6),
                    IsNullable: reader.GetBoolean(7),
                    IsIdentity: reader.GetBoolean(8),
                    IsComputed: reader.GetBoolean(9),
                    DefaultValue: reader.IsDBNull(10) ? null : reader.GetString(10),
                    Description: reader.IsDBNull(11) ? null : reader.GetString(11)));
            }

            return new DiscoverColumnsResponse(NormalizeColumns(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverColumnsResponse(Array.Empty<ColumnMetadataModel>());
        }
    }

    public async Task<DiscoverIndexesResponse> DiscoverIndexesAsync(
        DatabaseResource resource,
        DiscoverIndexesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverIndexesCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<IndexDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new IndexDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    IndexId: reader.GetInt32(1),
                    IndexName: reader.GetString(2),
                    SchemaName: reader.GetString(3),
                    TableName: reader.GetString(4),
                    IsPrimaryKey: reader.GetBoolean(5),
                    IsUnique: reader.GetBoolean(6),
                    IsClustered: reader.GetBoolean(7),
                    ColumnName: reader.GetString(8),
                    IsIncludedColumn: reader.GetBoolean(9),
                    KeyOrdinal: Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
                    IndexColumnId: Convert.ToInt32(reader.GetValue(11), CultureInfo.InvariantCulture),
                    FilterDefinition: reader.IsDBNull(12) ? null : reader.GetString(12)));
            }

            return new DiscoverIndexesResponse(NormalizeIndexes(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverIndexesResponse(Array.Empty<IndexMetadataModel>());
        }
    }

    public async Task<DiscoverPrimaryKeysResponse> DiscoverPrimaryKeysAsync(
        DatabaseResource resource,
        DiscoverPrimaryKeysRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverPrimaryKeysCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<PrimaryKeyDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new PrimaryKeyDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    ConstraintName: reader.GetString(1),
                    SchemaName: reader.GetString(2),
                    TableName: reader.GetString(3),
                    IsClustered: reader.GetBoolean(4),
                    ColumnName: reader.GetString(5),
                    KeyOrdinal: Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture)));
            }

            return new DiscoverPrimaryKeysResponse(NormalizePrimaryKeys(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverPrimaryKeysResponse(Array.Empty<PrimaryKeyConstraintModel>());
        }
    }

    public async Task<DiscoverViewsResponse> DiscoverViewsAsync(
        DatabaseResource resource,
        DiscoverViewsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverViewsCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<ViewDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new ViewDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    SchemaName: reader.GetString(1),
                    ViewName: reader.GetString(2),
                    HasDefinition: reader.GetBoolean(3)));
            }

            return new DiscoverViewsResponse(NormalizeViews(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverViewsResponse(Array.Empty<ViewObject>());
        }
    }

    public async Task<DiscoverTriggersResponse> DiscoverTriggersAsync(
        DatabaseResource resource,
        DiscoverTriggersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverTriggersCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<TriggerDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new TriggerDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    TriggerName: reader.GetString(1),
                    SchemaName: reader.GetString(2),
                    ParentSchemaName: reader.IsDBNull(3) ? null : reader.GetString(3),
                    ParentObjectName: reader.GetString(4),
                    ParentClass: reader.GetInt32(5),
                    IsDisabled: reader.GetBoolean(6),
                    IsInsteadOfTrigger: reader.GetBoolean(7),
                    HasDefinitionAvailable: reader.GetBoolean(8),
                    CreatedAt: reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    TriggerEventType: reader.IsDBNull(10) ? null : reader.GetString(10)));
            }

            return new DiscoverTriggersResponse(NormalizeTriggers(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverTriggersResponse(Array.Empty<TriggerMetadataModel>());
        }
    }

    public async Task<DiscoverStoredProceduresResponse> DiscoverStoredProceduresAsync(
        DatabaseResource resource,
        DiscoverStoredProceduresRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverStoredProceduresCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<StoredProcedureDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new StoredProcedureDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    SchemaName: reader.GetString(1),
                    ProcedureName: reader.GetString(2),
                    HasDefinitionAvailable: reader.GetBoolean(3),
                    CreatedAt: reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    ParameterId: reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ParameterName: reader.IsDBNull(6) ? null : reader.GetString(6),
                    ParameterDataType: reader.IsDBNull(7) ? null : reader.GetString(7),
                    ParameterMaxLength: reader.IsDBNull(8) ? null : reader.GetInt16(8),
                    ParameterPrecision: reader.IsDBNull(9) ? null : reader.GetByte(9),
                    ParameterScale: reader.IsDBNull(10) ? null : reader.GetByte(10),
                    ParameterIsOutput: reader.IsDBNull(11) ? null : reader.GetBoolean(11),
                    Definition: reader.IsDBNull(12) ? null : reader.GetString(12)));
            }

            return new DiscoverStoredProceduresResponse(GroupStoredProceduresBySchema(NormalizeStoredProcedures(rows)));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverStoredProceduresResponse(new Dictionary<string, IReadOnlyList<StoredProcedureMetadataModel>>(StringComparer.OrdinalIgnoreCase));
        }
    }

    public async Task<DiscoverFunctionsResponse> DiscoverFunctionsAsync(
        DatabaseResource resource,
        DiscoverFunctionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverFunctionsCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<FunctionDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new FunctionDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    SchemaName: reader.GetString(1),
                    FunctionName: reader.GetString(2),
                    FunctionTypeCode: reader.GetString(3),
                    ReturnType: reader.IsDBNull(4) ? null : reader.GetString(4),
                    ReturnTypeMaxLength: reader.IsDBNull(5) ? null : reader.GetInt16(5),
                    ReturnTypePrecision: reader.IsDBNull(6) ? null : reader.GetByte(6),
                    ReturnTypeScale: reader.IsDBNull(7) ? null : reader.GetByte(7),
                    HasDefinitionAvailable: reader.GetBoolean(8),
                    CreatedAt: reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    ParameterId: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    ParameterName: reader.IsDBNull(11) ? null : reader.GetString(11),
                    ParameterDataType: reader.IsDBNull(12) ? null : reader.GetString(12),
                    ParameterMaxLength: reader.IsDBNull(13) ? null : reader.GetInt16(13),
                    ParameterPrecision: reader.IsDBNull(14) ? null : reader.GetByte(14),
                    ParameterScale: reader.IsDBNull(15) ? null : reader.GetByte(15),
                    Definition: reader.IsDBNull(16) ? null : reader.GetString(16)));
            }

            return new DiscoverFunctionsResponse(GroupFunctionsBySchemaAndType(NormalizeFunctions(rows)));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverFunctionsResponse(
                new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadataModel>>>(StringComparer.OrdinalIgnoreCase));
        }
    }

    public async Task<DiscoverConstraintsResponse> DiscoverConstraintsAsync(
        DatabaseResource resource,
        DiscoverConstraintsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateDiscoverConstraintsCommand(connection, request);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<ConstraintDiscoveryRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new ConstraintDiscoveryRow(
                    ObjectId: reader.GetInt32(0),
                    ConstraintName: reader.GetString(1),
                    SchemaName: reader.GetString(2),
                    TableName: reader.GetString(3),
                    ColumnName: reader.IsDBNull(4) ? null : reader.GetString(4),
                    Definition: reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsDisabled: reader.GetBoolean(6),
                    ConstraintTypeCode: reader.GetString(7)));
            }

            return new DiscoverConstraintsResponse(NormalizeConstraints(rows));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverConstraintsResponse(Array.Empty<ConstraintMetadataModel>());
        }
    }

    public async Task<ObjectDefinitionResponse> GetDefinitionAsync(
        DatabaseResource resource,
        ObjectDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        if (request.ObjectType == DatabaseObjectType.Index)
        {
            return await GetIndexDefinitionAsync(resource, request, cancellationToken);
        }

        if (request.ObjectType is not (DatabaseObjectType.View or DatabaseObjectType.Procedure
            or DatabaseObjectType.Function or DatabaseObjectType.Trigger))
        {
            return new ObjectDefinitionResponse(
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Definition retrieval is not supported for this object type.");
        }

        if (!int.TryParse(request.ObjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var objectId))
        {
            return new ObjectDefinitionResponse(
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Invalid object identifier.");
        }

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateGetDefinitionCommand(connection, objectId);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null or DBNull)
            {
                return new ObjectDefinitionResponse(
                    Definition: null,
                    IsAvailable: false,
                    UnavailableReason: "Definition is not available for this object.");
            }

            var definition = (string)result;
            return new ObjectDefinitionResponse(
                Definition: definition,
                IsAvailable: true);
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new ObjectDefinitionResponse(
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Insufficient permissions to retrieve the object definition.");
        }
    }

    private async Task<ObjectDefinitionResponse> GetIndexDefinitionAsync(
        DatabaseResource resource,
        ObjectDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseIndexObjectId(request.ObjectId, out var tableObjectId, out var indexId))
        {
            return new ObjectDefinitionResponse(
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Invalid index object identifier.");
        }

        try
        {
            await using var connection = new SqlConnection(resource.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = CreateGetIndexDefinitionCommand(connection, tableObjectId, indexId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<IndexDefinitionRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new IndexDefinitionRow(
                    IndexName: reader.GetString(0),
                    SchemaName: reader.GetString(1),
                    TableName: reader.GetString(2),
                    IsUnique: reader.GetBoolean(3),
                    IsClustered: reader.GetBoolean(4),
                    IsPrimaryKey: reader.GetBoolean(5),
                    ColumnName: reader.GetString(6),
                    IsIncludedColumn: reader.GetBoolean(7),
                    KeyOrdinal: reader.GetInt32(8),
                    IndexColumnId: reader.GetInt32(9),
                    FilterDefinition: reader.IsDBNull(10) ? null : reader.GetString(10)));
            }

            if (rows.Count == 0)
            {
                return new ObjectDefinitionResponse(
                    Definition: null,
                    IsAvailable: false,
                    UnavailableReason: "Definition is not available for this object.");
            }

            return new ObjectDefinitionResponse(
                Definition: BuildIndexDefinition(rows),
                IsAvailable: true);
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new ObjectDefinitionResponse(
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Insufficient permissions to retrieve the object definition.");
        }
    }

    internal static TableObject CreateTableObject(int objectId, string schemaName, string tableName, long rowCount)
        => new(
            objectId: objectId.ToString(CultureInfo.InvariantCulture),
            schemaName: schemaName,
            objectName: tableName,
            providerMetadata: new Dictionary<string, object?>
            {
                ["objectId"] = objectId,
                ["rowCount"] = rowCount,
            });

    internal static SchemaObject CreateSchemaObject(int schemaId, string schemaName)
        => new(
            objectId: $"schema.{schemaName}",
            objectName: schemaName,
            providerMetadata: new Dictionary<string, object?>
            {
                ["schemaId"] = schemaId,
            });

    internal static ViewObject CreateViewObject(int objectId, string schemaName, string viewName, bool hasDefinition)
        => new(
            objectId: objectId.ToString(CultureInfo.InvariantCulture),
            schemaName: schemaName,
            objectName: viewName,
            hasDefinitionAvailable: hasDefinition,
            providerMetadata: new Dictionary<string, object?>
            {
                ["objectId"] = objectId,
            });

    internal static SqlCommand CreateDiscoverSchemasCommand(SqlConnection connection, bool includeSystemSchemas)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var command = new SqlCommand(DiscoverSchemasSql, connection);
        command.Parameters.Add("@IncludeSystemSchemas", SqlDbType.Bit).Value = includeSystemSchemas;
        return command;
    }

    internal static SqlCommand CreateDiscoverTablesCommand(SqlConnection connection, DiscoverTablesRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();

        var command = new SqlCommand(DiscoverTablesSql, connection);
        command.Parameters.Add("@IncludeSystemTables", SqlDbType.Bit).Value = request.IncludeSystemTables;
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        return command;
    }

    internal static IReadOnlyList<TableObject> NormalizeTables(IReadOnlyList<TableDiscoveryRow> rows)
        => rows
            .Select(row => CreateTableObject(row.ObjectId, row.SchemaName, row.TableName, row.RowCount))
            .ToList();

    internal static SqlCommand CreateDiscoverForeignKeysCommand(SqlConnection connection, DiscoverForeignKeysRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        var command = new SqlCommand(DiscoverForeignKeysSql, connection);

        string? parentSchemaName = string.IsNullOrWhiteSpace(request.ParentSchemaName)
            ? null
            : request.ParentSchemaName.Trim();
        string? parentTableName = string.IsNullOrWhiteSpace(request.ParentTableName)
            ? null
            : request.ParentTableName.Trim();

        command.Parameters.Add("@ParentSchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)parentSchemaName ?? DBNull.Value;
        command.Parameters.Add("@ParentTableName", SqlDbType.NVarChar, 128).Value =
            (object?)parentTableName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateDiscoverColumnsCommand(SqlConnection connection, DiscoverColumnsRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        var command = new SqlCommand(DiscoverColumnsSql, connection);
        command.Parameters.Add("@ObjectId", SqlDbType.Int).Value = DBNull.Value;

        if (!string.IsNullOrWhiteSpace(request.ObjectId))
        {
            if (!int.TryParse(request.ObjectId.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var objectId))
            {
                throw new ArgumentException("ObjectId must be a valid SQL Server object_id integer.", nameof(request));
            }

            command.Parameters["@ObjectId"].Value = objectId;
            command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value = DBNull.Value;
            command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 128).Value = DBNull.Value;
            command.Parameters.Add("@ObjectType", SqlDbType.NChar, 2).Value = DBNull.Value;
            return command;
        }

        var (schemaName, objectName) = ParseSchemaAndObjectName(request.FullyQualifiedName);
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value = schemaName;
        command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 128).Value = objectName;
        command.Parameters.Add("@ObjectType", SqlDbType.NChar, 2).Value = MapObjectType(request.ObjectType);
        return command;
    }

    internal static SqlCommand CreateDiscoverViewsCommand(SqlConnection connection, DiscoverViewsRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();

        var command = new SqlCommand(DiscoverViewsSql, connection);
        command.Parameters.Add("@IncludeSystemViews", SqlDbType.Bit).Value = request.IncludeSystemViews;
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateDiscoverStoredProceduresCommand(SqlConnection connection, DiscoverStoredProceduresRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();

        var command = new SqlCommand(DiscoverStoredProceduresSql, connection);
        command.Parameters.Add("@IncludeSystemProcedures", SqlDbType.Bit).Value = request.IncludeSystemProcedures;
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateDiscoverFunctionsCommand(SqlConnection connection, DiscoverFunctionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        var command = new SqlCommand(DiscoverFunctionsSql, connection);
        command.Parameters.Add("@IncludeSystemFunctions", SqlDbType.Bit).Value = request.IncludeSystemFunctions;
        return command;
    }

    internal static SqlCommand CreateDiscoverTriggersCommand(SqlConnection connection, DiscoverTriggersRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();
        string? parentObjectName = string.IsNullOrWhiteSpace(request.ParentObjectName)
            ? null
            : request.ParentObjectName.Trim();

        var command = new SqlCommand(DiscoverTriggersSql, connection);
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        command.Parameters.Add("@ParentObjectName", SqlDbType.NVarChar, 128).Value =
            (object?)parentObjectName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateDiscoverIndexesCommand(SqlConnection connection, DiscoverIndexesRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();
        string? tableName = string.IsNullOrWhiteSpace(request.TableName)
            ? null
            : request.TableName.Trim();

        var command = new SqlCommand(DiscoverIndexesSql, connection);
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value =
            (object?)tableName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateDiscoverPrimaryKeysCommand(SqlConnection connection, DiscoverPrimaryKeysRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();
        string? tableName = string.IsNullOrWhiteSpace(request.TableName)
            ? null
            : request.TableName.Trim();

        var command = new SqlCommand(DiscoverPrimaryKeysSql, connection);
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value =
            (object?)tableName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateDiscoverConstraintsCommand(SqlConnection connection, DiscoverConstraintsRequest request)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        string? schemaName = string.IsNullOrWhiteSpace(request.SchemaName)
            ? null
            : request.SchemaName.Trim();
        string? tableName = string.IsNullOrWhiteSpace(request.TableName)
            ? null
            : request.TableName.Trim();

        var command = new SqlCommand(DiscoverConstraintsSql, connection);
        command.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value =
            (object?)schemaName ?? DBNull.Value;
        command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value =
            (object?)tableName ?? DBNull.Value;
        return command;
    }

    internal static SqlCommand CreateGetDefinitionCommand(SqlConnection connection, int objectId)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var command = new SqlCommand(GetObjectDefinitionSql, connection);
        command.Parameters.Add("@ObjectId", SqlDbType.Int).Value = objectId;
        return command;
    }

    internal static SqlCommand CreateGetIndexDefinitionCommand(SqlConnection connection, int objectId, int indexId)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var command = new SqlCommand(GetIndexDefinitionSql, connection);
        command.Parameters.Add("@ObjectId", SqlDbType.Int).Value = objectId;
        command.Parameters.Add("@IndexId", SqlDbType.Int).Value = indexId;
        return command;
    }

    internal static string BuildIndexDefinition(IReadOnlyList<IndexDefinitionRow> rows)
    {
        var first = rows[0];
        var keyColumns = rows
            .Where(row => !row.IsIncludedColumn)
            .OrderBy(row => row.KeyOrdinal)
            .ThenBy(row => row.IndexColumnId)
            .Select(row => $"[{row.ColumnName}]")
            .ToList();
        var includedColumns = rows
            .Where(row => row.IsIncludedColumn)
            .OrderBy(row => row.IndexColumnId)
            .Select(row => $"[{row.ColumnName}]")
            .ToList();

        var sb = new System.Text.StringBuilder();

        if (first.IsPrimaryKey)
        {
            sb.Append("PRIMARY KEY ");
        }
        else
        {
            if (first.IsUnique)
            {
                sb.Append("UNIQUE ");
            }

            sb.Append("INDEX ");
            sb.Append('[');
            sb.Append(first.IndexName);
            sb.Append("] ");
        }

        sb.Append(first.IsClustered ? "CLUSTERED " : "NONCLUSTERED ");
        sb.Append("ON [");
        sb.Append(first.SchemaName);
        sb.Append("].[");
        sb.Append(first.TableName);
        sb.Append("] (");
        sb.Append(string.Join(", ", keyColumns));
        sb.Append(')');

        if (includedColumns.Count > 0)
        {
            sb.Append(" INCLUDE (");
            sb.Append(string.Join(", ", includedColumns));
            sb.Append(')');
        }

        if (!string.IsNullOrWhiteSpace(first.FilterDefinition))
        {
            sb.Append(" WHERE ");
            sb.Append(first.FilterDefinition);
        }

        return sb.ToString();
    }

    internal static bool TryParseIndexObjectId(string objectId, out int tableObjectId, out int indexId)
    {
        tableObjectId = 0;
        indexId = 0;

        if (string.IsNullOrWhiteSpace(objectId))
        {
            return false;
        }

        var parts = objectId.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out tableObjectId)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out indexId);
    }

    internal static IReadOnlyList<ViewObject> NormalizeViews(IReadOnlyList<ViewDiscoveryRow> rows)
        => rows
            .Select(row => CreateViewObject(row.ObjectId, row.SchemaName, row.ViewName, row.HasDefinition))
            .ToList();

    internal static IReadOnlyList<TriggerMetadataModel> NormalizeTriggers(IReadOnlyList<TriggerDiscoveryRow> rows)
        => rows
            .GroupBy(row => row.ObjectId)
            .Select(group =>
            {
                var first = group.First();
                var triggerType = first.IsInsteadOfTrigger ? TriggerType.InsteadOf : TriggerType.After;

                var eventTypes = group
                    .Select(row => row.TriggerEventType)
                    .Where(eventType => !string.IsNullOrWhiteSpace(eventType))
                    .Select(eventType => eventType!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (eventTypes.Contains("INSERT"))
                {
                    triggerType |= TriggerType.Insert;
                }

                if (eventTypes.Contains("UPDATE"))
                {
                    triggerType |= TriggerType.Update;
                }

                if (eventTypes.Contains("DELETE"))
                {
                    triggerType |= TriggerType.Delete;
                }

                return new TriggerMetadataModel(
                    TriggerName: first.TriggerName,
                    SchemaName: first.SchemaName,
                    ParentObjectName: first.ParentObjectName,
                    ParentObjectType: MapTriggerParentObjectType(first.ParentClass),
                    TriggerType: triggerType,
                    IsEnabled: !first.IsDisabled,
                    HasDefinitionAvailable: first.HasDefinitionAvailable,
                    ObjectId: first.ObjectId.ToString(CultureInfo.InvariantCulture),
                    CreatedAt: first.CreatedAt is null
                        ? null
                        : new DateTimeOffset(DateTime.SpecifyKind(first.CreatedAt.Value, DateTimeKind.Utc)),
                    ParentSchemaName: first.ParentSchemaName);
            })
            .ToList();

    internal static IReadOnlyList<StoredProcedureMetadataModel> NormalizeStoredProcedures(IReadOnlyList<StoredProcedureDiscoveryRow> rows)
        => rows
            .GroupBy(row => row.ObjectId)
            .Select(group =>
            {
                var first = group.First();
                var parameterDefaults = ParseRoutineParameterDefaults(first.Definition);
                var parameters = group
                    .Where(row => row.ParameterId.HasValue && !string.IsNullOrWhiteSpace(row.ParameterName))
                    .OrderBy(row => row.ParameterId)
                    .Select(row => new StoredProcedureParameterMetadata(
                        Name: row.ParameterName!,
                        DataType: FormatRoutineDataType(
                            row.ParameterDataType,
                            row.ParameterMaxLength,
                            row.ParameterPrecision,
                            row.ParameterScale),
                        Direction: row.ParameterIsOutput is true
                            ? RoutineParameterDirection.Output
                            : RoutineParameterDirection.Input,
                        HasDefault: parameterDefaults.Contains(row.ParameterName!)))
                    .ToList();

                return new StoredProcedureMetadataModel(
                    SchemaName: first.SchemaName,
                    ProcedureName: first.ProcedureName,
                    ObjectId: first.ObjectId.ToString(CultureInfo.InvariantCulture),
                    HasDefinitionAvailable: first.HasDefinitionAvailable,
                    Parameters: parameters.Count == 0 ? null : parameters,
                    CreatedAt: first.CreatedAt is null
                        ? null
                        : new DateTimeOffset(DateTime.SpecifyKind(first.CreatedAt.Value, DateTimeKind.Utc)));
            })
            .ToList();

    internal static IReadOnlyDictionary<string, IReadOnlyList<StoredProcedureMetadataModel>> GroupStoredProceduresBySchema(
        IReadOnlyList<StoredProcedureMetadataModel> procedures)
        => procedures
            .GroupBy(procedure => procedure.SchemaName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<StoredProcedureMetadataModel>)group
                    .OrderBy(procedure => procedure.ProcedureName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyList<FunctionMetadataModel> NormalizeFunctions(IReadOnlyList<FunctionDiscoveryRow> rows)
        => rows
            .GroupBy(row => row.ObjectId)
            .Select(group =>
            {
                var first = group.First();
                var parameters = group
                    .Where(row => row.ParameterId.HasValue && !string.IsNullOrWhiteSpace(row.ParameterName))
                    .OrderBy(row => row.ParameterId)
                    .Select(row => new FunctionParameterMetadata(
                        Name: row.ParameterName!,
                        DataType: FormatRoutineDataType(
                            row.ParameterDataType,
                            row.ParameterMaxLength,
                            row.ParameterPrecision,
                            row.ParameterScale)))
                    .ToList();

                return new FunctionMetadataModel(
                    SchemaName: first.SchemaName,
                    FunctionName: first.FunctionName,
                    FunctionType: MapFunctionType(first.FunctionTypeCode),
                    ObjectId: first.ObjectId.ToString(CultureInfo.InvariantCulture),
                    ReturnType: FormatRoutineDataType(
                        first.ReturnType,
                        first.ReturnTypeMaxLength,
                        first.ReturnTypePrecision,
                        first.ReturnTypeScale),
                    HasDefinitionAvailable: first.HasDefinitionAvailable,
                    CreatedAt: first.CreatedAt is null
                        ? null
                        : new DateTimeOffset(DateTime.SpecifyKind(first.CreatedAt.Value, DateTimeKind.Utc)),
                    Parameters: parameters.Count == 0 ? null : parameters);
            })
            .ToList();

    private static string FormatRoutineDataType(
        string? dataType,
        short? maxLength,
        byte? precision,
        byte? scale)
    {
        if (string.IsNullOrWhiteSpace(dataType))
        {
            return "sql_variant";
        }

        var normalized = dataType.Trim();

        if (maxLength.HasValue)
        {
            if (normalized.Equals("nvarchar", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("nchar", StringComparison.OrdinalIgnoreCase))
            {
                return $"{normalized}({FormatLength(maxLength.Value / 2)})";
            }

            if (normalized.Equals("varchar", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("char", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("varbinary", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("binary", StringComparison.OrdinalIgnoreCase))
            {
                return $"{normalized}({FormatLength(maxLength.Value)})";
            }
        }

        if ((normalized.Equals("decimal", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("numeric", StringComparison.OrdinalIgnoreCase))
            && precision.HasValue
            && scale.HasValue)
        {
            return $"{normalized}({precision.Value},{scale.Value})";
        }

        if ((normalized.Equals("datetime2", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("datetimeoffset", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("time", StringComparison.OrdinalIgnoreCase))
            && scale.HasValue)
        {
            return $"{normalized}({scale.Value})";
        }

        return normalized;
    }

    private static string FormatLength(int length)
        => length < 0 ? "max" : length.ToString(CultureInfo.InvariantCulture);

    private static HashSet<string> ParseRoutineParameterDefaults(string? definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
        {
            return [];
        }

        var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var signature = TrimToRoutineSignature(definition);
        var span = signature.AsSpan();

        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] != '@')
            {
                continue;
            }

            var nameEnd = index + 1;
            while (nameEnd < span.Length && (char.IsLetterOrDigit(span[nameEnd]) || span[nameEnd] == '_' || span[nameEnd] == '#'))
            {
                nameEnd++;
            }

            if (nameEnd == index + 1)
            {
                continue;
            }

            var parameterName = span[index..nameEnd].ToString();
            var cursor = nameEnd;
            var depth = 0;
            var inString = false;
            var hasDefault = false;

            while (cursor < span.Length)
            {
                var current = span[cursor];

                if (current == '\'')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (current == '(')
                    {
                        depth++;
                    }
                    else if (current == ')' && depth > 0)
                    {
                        depth--;
                    }
                    else if (current == '=' && depth == 0)
                    {
                        hasDefault = true;
                    }
                    else if (current == ',' && depth == 0)
                    {
                        break;
                    }
                }

                cursor++;
            }

            if (hasDefault)
            {
                defaults.Add(parameterName);
            }

            index = cursor;
        }

        return defaults;
    }

    private static string TrimToRoutineSignature(string definition)
    {
        var firstParameterIndex = definition.IndexOf('@');
        if (firstParameterIndex < 0)
        {
            return definition;
        }

        var asIndex = definition.IndexOf(" AS ", firstParameterIndex, StringComparison.OrdinalIgnoreCase);
        var returnsIndex = definition.IndexOf(" RETURNS ", firstParameterIndex, StringComparison.OrdinalIgnoreCase);
        var endIndex = asIndex >= 0 && returnsIndex >= 0
            ? Math.Min(asIndex, returnsIndex)
            : asIndex >= 0
                ? asIndex
                : returnsIndex >= 0
                    ? returnsIndex
                    : definition.Length;

        return definition[..endIndex];
    }

    internal static IReadOnlyDictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadataModel>>> GroupFunctionsBySchemaAndType(
        IReadOnlyList<FunctionMetadataModel> functions)
        => functions
            .GroupBy(function => function.SchemaName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                schemaGroup => schemaGroup.Key,
                schemaGroup => (IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadataModel>>)schemaGroup
                    .GroupBy(function => function.FunctionType)
                    .OrderBy(typeGroup => typeGroup.Key)
                    .ToDictionary(
                        typeGroup => typeGroup.Key,
                        typeGroup => (IReadOnlyList<FunctionMetadataModel>)typeGroup
                            .OrderBy(function => function.FunctionName, StringComparer.OrdinalIgnoreCase)
                            .ToList()),
                StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyList<ColumnMetadataModel> NormalizeColumns(
        IReadOnlyList<ColumnDiscoveryRow> rows)
        => rows
            .OrderBy(row => row.ColumnId)
            .Select(row => new ColumnMetadataModel(
                Name: row.Name,
                Ordinal: row.ColumnId,
                DataType: row.DataType,
                MaxLength: row.MaxLength,
                Precision: row.Precision,
                Scale: row.Scale,
                IsNullable: row.IsNullable,
                IsIdentity: row.IsIdentity,
                IsComputed: row.IsComputed,
                DefaultValue: row.DefaultValue,
                Description: row.Description,
                ProviderMetadata: new Dictionary<string, object?>
                {
                    ["objectId"] = row.ObjectId,
                    ["columnId"] = row.ColumnId,
                }))
            .ToList();

    internal static IReadOnlyList<IndexMetadataModel> NormalizeIndexes(
        IReadOnlyList<IndexDiscoveryRow> rows)
        => rows
            .GroupBy(row => (row.ObjectId, row.IndexId))
            .Select(group =>
            {
                var first = group.First();
                var columns = group
                    .Where(row => !row.IsIncludedColumn)
                    .OrderBy(row => row.KeyOrdinal)
                    .ThenBy(row => row.IndexColumnId)
                    .Select(row => row.ColumnName)
                    .ToList();
                var includedColumns = group
                    .Where(row => row.IsIncludedColumn)
                    .OrderBy(row => row.IndexColumnId)
                    .Select(row => row.ColumnName)
                    .ToList();

                return new IndexMetadataModel(
                    IndexName: first.IndexName,
                    TableName: $"{first.SchemaName}.{first.TableName}",
                    SchemaName: first.SchemaName,
                    IsPrimaryKey: first.IsPrimaryKey,
                    IsUnique: first.IsUnique,
                    IsClustered: first.IsClustered,
                    Columns: columns,
                    IncludedColumns: includedColumns,
                    FilterDefinition: first.FilterDefinition,
                    ObjectId: CreateIndexObjectId(first.ObjectId, first.IndexId));
            })
            .ToList();

    internal static IReadOnlyList<ForeignKeyConstraintModel> NormalizeForeignKeyConstraints(
        IReadOnlyList<ForeignKeyDiscoveryRow> rows)
        => rows
            .GroupBy(row => row.ObjectId)
            .Select(group =>
            {
                var first = group.First();
                var keyColumns = group
                    .OrderBy(row => row.ConstraintColumnId)
                    .Select(row => new ForeignKeyColumnMapping(
                        ParentColumnName: row.ParentColumnName,
                        ReferencedColumnName: row.ReferencedColumnName))
                    .ToList();

                return new ForeignKeyConstraintModel(
                    ConstraintName: first.ConstraintName,
                    ParentTableName: $"{first.ParentSchemaName}.{first.ParentTableName}",
                    ParentSchemaName: first.ParentSchemaName,
                    ReferencedTableName: $"{first.ReferencedSchemaName}.{first.ReferencedTableName}",
                    ReferencedSchemaName: first.ReferencedSchemaName,
                    KeyColumns: keyColumns,
                    OnDeleteBehavior: MapReferentialAction(first.DeleteReferentialAction),
                    OnUpdateBehavior: MapReferentialAction(first.UpdateReferentialAction),
                    IsDisabled: first.IsDisabled,
                    ObjectId: first.ObjectId.ToString(CultureInfo.InvariantCulture));
            })
            .ToList();

    internal static IReadOnlyList<PrimaryKeyConstraintModel> NormalizePrimaryKeys(
        IReadOnlyList<PrimaryKeyDiscoveryRow> rows)
        => rows
            .GroupBy(row => row.ObjectId)
            .Select(group =>
            {
                var first = group.First();
                var keyColumns = group
                    .OrderBy(row => row.KeyOrdinal)
                    .Select(row => row.ColumnName)
                    .ToList();

                return new PrimaryKeyConstraintModel(
                    ConstraintName: first.ConstraintName,
                    TableName: $"{first.SchemaName}.{first.TableName}",
                    SchemaName: first.SchemaName,
                    KeyColumns: keyColumns,
                    IsClustered: first.IsClustered,
                    ObjectId: first.ObjectId.ToString(CultureInfo.InvariantCulture));
            })
            .ToList();

    internal static IReadOnlyList<ConstraintMetadataModel> NormalizeConstraints(
        IReadOnlyList<ConstraintDiscoveryRow> rows)
        => rows
            .Select(row => new ConstraintMetadataModel(
                ConstraintName: row.ConstraintName,
                ConstraintType: MapConstraintType(row.ConstraintTypeCode),
                TableName: $"{row.SchemaName}.{row.TableName}",
                SchemaName: row.SchemaName,
                ColumnName: row.ColumnName,
                Definition: row.Definition,
                IsDisabled: row.IsDisabled,
                ObjectId: row.ObjectId.ToString(CultureInfo.InvariantCulture)))
            .ToList();

    internal static ConstraintType MapConstraintType(string typeCode)
        => typeCode switch
        {
            "D" => ConstraintType.Default,
            "C" => ConstraintType.Check,
            "U" => ConstraintType.Unique,
            _ => throw new ArgumentException($"Unknown constraint type code: {typeCode}", nameof(typeCode)),
        };

    internal static ReferentialActionBehavior MapReferentialAction(int action)
        => action switch
        {
            1 => ReferentialActionBehavior.Cascade,
            2 => ReferentialActionBehavior.SetNull,
            3 => ReferentialActionBehavior.SetDefault,
            0 or 4 => ReferentialActionBehavior.NoAction,
            _ => ReferentialActionBehavior.NoAction,
        };

    internal static TriggerParentObjectType MapTriggerParentObjectType(int parentClass)
        => parentClass == 0
            ? TriggerParentObjectType.Database
            : TriggerParentObjectType.Table;

    internal static FunctionType MapFunctionType(string typeCode)
        => typeCode switch
        {
            "FN" => FunctionType.Scalar,
            "TF" => FunctionType.TableValued,
            "IF" => FunctionType.InlineTableValued,
            _ => throw new ArgumentException($"Unknown function type code: {typeCode}", nameof(typeCode)),
        };

    private static (string SchemaName, string ObjectName) ParseSchemaAndObjectName(string? fullyQualifiedName)
    {
        if (string.IsNullOrWhiteSpace(fullyQualifiedName))
        {
            throw new ArgumentException("Either ObjectId or FullyQualifiedName must be provided.", nameof(fullyQualifiedName));
        }

        var parts = fullyQualifiedName
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            1 => ("dbo", parts[0]),
            >= 2 => (parts[^2], parts[^1]),
            _ => throw new ArgumentException(
                "FullyQualifiedName must be in the format [schema].[object] or [database].[schema].[object].",
                nameof(fullyQualifiedName)),
        };
    }

    private static string MapObjectType(DatabaseObjectType objectType)
        => objectType switch
        {
            DatabaseObjectType.Table => "U",
            DatabaseObjectType.View => "V",
            _ => throw new ArgumentException("ObjectType must be Table or View.", nameof(objectType)),
        };

    internal static bool HasInsufficientSchemaAccess(SqlException exception)
        => exception.Number is 229 or 230 or 297 or 300 or 916;

    private static string CreateIndexObjectId(int objectId, int indexId)
        => FormattableString.Invariant($"{objectId}:{indexId}");

    internal readonly record struct TableDiscoveryRow(
        int ObjectId,
        string SchemaName,
        string TableName,
        long RowCount);

    internal readonly record struct ColumnDiscoveryRow(
        int ObjectId,
        int ColumnId,
        string Name,
        string DataType,
        short? MaxLength,
        byte? Precision,
        byte? Scale,
        bool IsNullable,
        bool IsIdentity,
        bool IsComputed,
        string? DefaultValue,
        string? Description);

    internal readonly record struct IndexDiscoveryRow(
        int ObjectId,
        int IndexId,
        string IndexName,
        string SchemaName,
        string TableName,
        bool IsPrimaryKey,
        bool IsUnique,
        bool IsClustered,
        string ColumnName,
        bool IsIncludedColumn,
        int KeyOrdinal,
        int IndexColumnId,
        string? FilterDefinition);

    internal readonly record struct ForeignKeyDiscoveryRow(
        int ObjectId,
        string ConstraintName,
        string ParentSchemaName,
        string ParentTableName,
        string ReferencedSchemaName,
        string ReferencedTableName,
        string ParentColumnName,
        string ReferencedColumnName,
        int ConstraintColumnId,
        int DeleteReferentialAction,
        int UpdateReferentialAction,
        bool IsDisabled);

    internal readonly record struct PrimaryKeyDiscoveryRow(
        int ObjectId,
        string ConstraintName,
        string SchemaName,
        string TableName,
        bool IsClustered,
        string ColumnName,
        int KeyOrdinal);

    internal readonly record struct ViewDiscoveryRow(
        int ObjectId,
        string SchemaName,
        string ViewName,
        bool HasDefinition);

    internal readonly record struct TriggerDiscoveryRow(
        int ObjectId,
        string TriggerName,
        string SchemaName,
        string ParentObjectName,
        int ParentClass,
        bool IsDisabled,
        bool IsInsteadOfTrigger,
        bool HasDefinitionAvailable,
        DateTime? CreatedAt,
        string? TriggerEventType,
        string? ParentSchemaName = null);

    internal readonly record struct ConstraintDiscoveryRow(
        int ObjectId,
        string ConstraintName,
        string SchemaName,
        string TableName,
        string? ColumnName,
        string? Definition,
        bool IsDisabled,
        string ConstraintTypeCode);

    internal readonly record struct StoredProcedureDiscoveryRow(
        int ObjectId,
        string SchemaName,
        string ProcedureName,
        bool HasDefinitionAvailable,
        DateTime? CreatedAt,
        int? ParameterId,
        string? ParameterName,
        string? ParameterDataType,
        short? ParameterMaxLength = null,
        byte? ParameterPrecision = null,
        byte? ParameterScale = null,
        bool? ParameterIsOutput = null,
        string? Definition = null);

    internal readonly record struct FunctionDiscoveryRow(
        int ObjectId,
        string SchemaName,
        string FunctionName,
        string FunctionTypeCode,
        string? ReturnType,
        bool HasDefinitionAvailable,
        DateTime? CreatedAt,
        short? ReturnTypeMaxLength = null,
        byte? ReturnTypePrecision = null,
        byte? ReturnTypeScale = null,
        int? ParameterId = null,
        string? ParameterName = null,
        string? ParameterDataType = null,
        short? ParameterMaxLength = null,
        byte? ParameterPrecision = null,
        byte? ParameterScale = null,
        string? Definition = null);

    internal readonly record struct IndexDefinitionRow(
        string IndexName,
        string SchemaName,
        string TableName,
        bool IsUnique,
        bool IsClustered,
        bool IsPrimaryKey,
        string ColumnName,
        bool IsIncludedColumn,
        int KeyOrdinal,
        int IndexColumnId,
        string? FilterDefinition);
}
