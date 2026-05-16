using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using ColumnMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata;
using ConstraintMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ConstraintMetadata;
using ForeignKeyConstraintModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ForeignKeyConstraint;
using IndexMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.IndexMetadata;
using PrimaryKeyConstraintModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.PrimaryKeyConstraint;
using StoredProcedureMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.StoredProcedureMetadata;
using TriggerMetadataModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.TriggerMetadata;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider, ISchemaDiscoveryProvider, IForeignKeyDiscoveryProvider, IColumnDiscoveryProvider, IIndexDiscoveryProvider, IPrimaryKeyDiscoveryProvider, ITableDiscoveryProvider, IViewDiscoveryProvider, IStoredProcedureDiscoveryProvider, ITriggerDiscoveryProvider, IConstraintDiscoveryProvider
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
            typ.name AS parameter_type
        FROM sys.procedures AS p
        LEFT JOIN sys.parameters AS prm ON p.object_id = prm.object_id
        LEFT JOIN sys.types AS typ ON prm.user_type_id = typ.user_type_id
        WHERE (@IncludeSystemProcedures = 1 OR p.is_ms_shipped = 0)
          AND (@SchemaName IS NULL OR SCHEMA_NAME(p.schema_id) = @SchemaName)
        ORDER BY schema_name, procedure_name, prm.parameter_id;
        """;

    private const string DiscoverTriggersSql = """
        SELECT
            t.object_id,
            t.name AS trigger_name,
            SCHEMA_NAME(t.schema_id) AS schema_name,
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

    public Task<QueryResult> ExecuteQueryAsync(
        DatabaseResource resource,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(
            new QueryResult(
                Columns: Array.Empty<string>(),
                Rows: Array.Empty<IReadOnlyDictionary<string, object?>>(),
                RowCount: 0,
                Duration: TimeSpan.Zero));

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
                    ConstraintColumnId: reader.GetInt32(8),
                    DeleteReferentialAction: reader.GetInt32(9),
                    UpdateReferentialAction: reader.GetInt32(10),
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
                    KeyOrdinal: reader.GetInt32(10),
                    IndexColumnId: reader.GetInt32(11),
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
                    KeyOrdinal: reader.GetInt32(6)));
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
                    ParentObjectName: reader.GetString(3),
                    ParentClass: reader.GetInt32(4),
                    IsDisabled: reader.GetBoolean(5),
                    IsInsteadOfTrigger: reader.GetBoolean(6),
                    HasDefinitionAvailable: reader.GetBoolean(7),
                    CreatedAt: reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    TriggerEventType: reader.IsDBNull(9) ? null : reader.GetString(9)));
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
                    ParameterDataType: reader.IsDBNull(7) ? null : reader.GetString(7)));
            }

            return new DiscoverStoredProceduresResponse(GroupStoredProceduresBySchema(NormalizeStoredProcedures(rows)));
        }
        catch (SqlException ex) when (HasInsufficientSchemaAccess(ex))
        {
            return new DiscoverStoredProceduresResponse(new Dictionary<string, IReadOnlyList<StoredProcedureMetadataModel>>(StringComparer.OrdinalIgnoreCase));
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
                        : new DateTimeOffset(DateTime.SpecifyKind(first.CreatedAt.Value, DateTimeKind.Utc)));
            })
            .ToList();

    internal static IReadOnlyList<StoredProcedureMetadataModel> NormalizeStoredProcedures(IReadOnlyList<StoredProcedureDiscoveryRow> rows)
        => rows
            .GroupBy(row => row.ObjectId)
            .Select(group =>
            {
                var first = group.First();
                var parameters = group
                    .Where(row => row.ParameterId.HasValue && !string.IsNullOrWhiteSpace(row.ParameterName))
                    .OrderBy(row => row.ParameterId)
                    .Select(row => new StoredProcedureParameterMetadata(
                        Name: row.ParameterName!,
                        DataType: string.IsNullOrWhiteSpace(row.ParameterDataType) ? "sql_variant" : row.ParameterDataType!))
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

    private static bool HasInsufficientSchemaAccess(SqlException exception)
        => exception.Number is 229 or 916;

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
        string? TriggerEventType);

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
        string? ParameterDataType);
}
