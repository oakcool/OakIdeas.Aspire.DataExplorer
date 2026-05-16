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

public interface ISchemaDiscoveryProvider
{
    Task<DiscoverSchemasResponse> DiscoverSchemasAsync(
        DatabaseResource resource,
        DiscoverSchemasRequest request,
        CancellationToken cancellationToken);
}

public interface IForeignKeyDiscoveryProvider
{
    Task<DiscoverForeignKeysResponse> DiscoverForeignKeysAsync(
        DatabaseResource resource,
        DiscoverForeignKeysRequest request,
        CancellationToken cancellationToken);
}

public interface IColumnDiscoveryProvider
{
    Task<DiscoverColumnsResponse> DiscoverColumnsAsync(
        DatabaseResource resource,
        DiscoverColumnsRequest request,
        CancellationToken cancellationToken);
}

public interface IIndexDiscoveryProvider
{
    Task<DiscoverIndexesResponse> DiscoverIndexesAsync(
        DatabaseResource resource,
        DiscoverIndexesRequest request,
        CancellationToken cancellationToken);
}

public interface IPrimaryKeyDiscoveryProvider
{
    Task<DiscoverPrimaryKeysResponse> DiscoverPrimaryKeysAsync(
        DatabaseResource resource,
        DiscoverPrimaryKeysRequest request,
        CancellationToken cancellationToken);
}

public interface ITableDiscoveryProvider
{
    Task<DiscoverTablesResponse> DiscoverTablesAsync(
        DatabaseResource resource,
        DiscoverTablesRequest request,
        CancellationToken cancellationToken);
}

public interface IViewDiscoveryProvider
{
    Task<DiscoverViewsResponse> DiscoverViewsAsync(
        DatabaseResource resource,
        DiscoverViewsRequest request,
        CancellationToken cancellationToken);
}

public interface IStoredProcedureDiscoveryProvider
{
    Task<DiscoverStoredProceduresResponse> DiscoverStoredProceduresAsync(
        DatabaseResource resource,
        DiscoverStoredProceduresRequest request,
        CancellationToken cancellationToken);
}

public interface IFunctionDiscoveryProvider
{
    Task<DiscoverFunctionsResponse> DiscoverFunctionsAsync(
        DatabaseResource resource,
        DiscoverFunctionsRequest request,
        CancellationToken cancellationToken);
}

public interface ITriggerDiscoveryProvider
{
    Task<DiscoverTriggersResponse> DiscoverTriggersAsync(
        DatabaseResource resource,
        DiscoverTriggersRequest request,
        CancellationToken cancellationToken);
}

public interface IConstraintDiscoveryProvider
{
    Task<DiscoverConstraintsResponse> DiscoverConstraintsAsync(
        DatabaseResource resource,
        DiscoverConstraintsRequest request,
        CancellationToken cancellationToken);
}

public interface IObjectDefinitionProvider
{
    Task<ObjectDefinitionResponse> GetDefinitionAsync(
        DatabaseResource resource,
        ObjectDefinitionRequest request,
        CancellationToken cancellationToken);
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
