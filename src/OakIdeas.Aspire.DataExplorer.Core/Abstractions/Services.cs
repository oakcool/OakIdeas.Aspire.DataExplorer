using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IMetadataProvider
{
    DatabaseProviderType ProviderType { get; }

    ProviderCapabilities Capabilities { get; }

    Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(
        DatabaseResource resource,
        CancellationToken cancellationToken);

    Task<QueryResult> ExecuteQueryAsync(
        DatabaseResource resource,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken);
}

public interface IProviderFactory
{
    IMetadataProvider Create(DatabaseProviderType providerType);

    bool TryCreate(DatabaseProviderType providerType, out IMetadataProvider? provider);
}

public interface IDatabaseProvider : IMetadataProvider
{
    string ProviderName { get; }

    bool CanHandle(DatabaseResource resource);
}

public interface ITableDataService
{
    Task<TablePageResult> GetRowsAsync(
        DatabaseResource resource,
        TableRowsRequest request,
        CancellationToken cancellationToken);

    Task<RowOperationResult> InsertRowAsync(
        DatabaseResource resource,
        InsertRowRequest request,
        CancellationToken cancellationToken);

    Task<RowOperationResult> UpdateRowAsync(
        DatabaseResource resource,
        UpdateRowRequest request,
        CancellationToken cancellationToken);

    Task<RowOperationResult> DeleteRowAsync(
        DatabaseResource resource,
        DeleteRowRequest request,
        CancellationToken cancellationToken);
}

public interface IAspireResourceDiscovery
{
    Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
        DiscoverResourcesRequest request,
        CancellationToken cancellationToken);
}

public interface ISelectedDatabaseService
{
    event EventHandler<SelectedDatabaseContext?>? SelectionChanged;

    Task<SelectDatabaseResponse> SelectDatabaseAsync(
        string resourceId,
        CancellationToken cancellationToken);

    Task<SelectedDatabaseContext?> GetSelectedDatabaseAsync(
        CancellationToken cancellationToken);

    Task ClearSelectionAsync(CancellationToken cancellationToken);

    Task<bool> IsSelectedAsync(CancellationToken cancellationToken);
}
