using System.Data;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider, ISchemaDiscoveryProvider
{
    private static readonly HashSet<string> SystemSchemas = new(StringComparer.OrdinalIgnoreCase)
    {
        "dbo",
        "guest",
        "INFORMATION_SCHEMA",
        "sys",
    };

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

            await using var command = new SqlCommand(DiscoverSchemasSql, connection);
            command.Parameters.Add("@IncludeSystemSchemas", SqlDbType.Bit).Value = request.IncludeSystemSchemas;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var discovered = new List<SchemaObject>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var schemaId = reader.GetInt32(0);
                var schemaName = reader.GetString(1);

                if (!request.IncludeSystemSchemas && IsSystemSchema(schemaName))
                {
                    continue;
                }

                discovered.Add(CreateSchemaObject(schemaId, schemaName));
            }

            return BuildDiscoverSchemasResponse(discovered, request.IncludeSystemSchemas);
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

    internal static bool IsSystemSchema(string schemaName)
        => SystemSchemas.Contains(schemaName);

    internal static SchemaObject CreateSchemaObject(int schemaId, string schemaName)
        => new(
            objectId: $"schema.{schemaName}",
            objectName: schemaName,
            providerMetadata: new Dictionary<string, object?>
            {
                ["schemaId"] = schemaId,
            });

    internal static DiscoverSchemasResponse BuildDiscoverSchemasResponse(
        IReadOnlyList<SchemaObject> schemas,
        bool includeSystemSchemas)
    {
        var filtered = includeSystemSchemas
            ? schemas
            : schemas.Where(schema => !IsSystemSchema(schema.ObjectName)).ToList();

        var ordered = filtered
            .OrderBy(schema => schema.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DiscoverSchemasResponse(ordered);
    }

    private static bool HasInsufficientSchemaAccess(SqlException exception)
        => exception.Number is 229 or 916;
}
