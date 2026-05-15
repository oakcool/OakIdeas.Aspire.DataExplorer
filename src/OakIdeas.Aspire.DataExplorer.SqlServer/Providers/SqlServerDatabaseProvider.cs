using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
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

    public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(
        DatabaseResource resource,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SchemaMetadata>>(Array.Empty<SchemaMetadata>());

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
}
