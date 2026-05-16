using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using ForeignKeyConstraintModel = OakIdeas.Aspire.DataExplorer.Contracts.Models.ForeignKeyConstraint;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider, ISchemaDiscoveryProvider, IForeignKeyDiscoveryProvider
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

    internal static SchemaObject CreateSchemaObject(int schemaId, string schemaName)
        => new(
            objectId: $"schema.{schemaName}",
            objectName: schemaName,
            providerMetadata: new Dictionary<string, object?>
            {
                ["schemaId"] = schemaId,
            });

    internal static SqlCommand CreateDiscoverSchemasCommand(SqlConnection connection, bool includeSystemSchemas)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var command = new SqlCommand(DiscoverSchemasSql, connection);
        command.Parameters.Add("@IncludeSystemSchemas", SqlDbType.Bit).Value = includeSystemSchemas;
        return command;
    }

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

    internal static ReferentialActionBehavior MapReferentialAction(int action)
        => action switch
        {
            1 => ReferentialActionBehavior.Cascade,
            2 => ReferentialActionBehavior.SetNull,
            3 => ReferentialActionBehavior.SetDefault,
            0 or 4 => ReferentialActionBehavior.NoAction,
            _ => ReferentialActionBehavior.NoAction,
        };

    private static bool HasInsufficientSchemaAccess(SqlException exception)
        => exception.Number is 229 or 916;

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
}
