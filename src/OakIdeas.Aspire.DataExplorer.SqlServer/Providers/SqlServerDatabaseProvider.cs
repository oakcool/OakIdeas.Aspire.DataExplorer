using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public string ProviderName => "sqlserver";

    public bool CanHandle(DatabaseResource resource)
        => resource.Provider.Contains("sql", StringComparison.OrdinalIgnoreCase);

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
