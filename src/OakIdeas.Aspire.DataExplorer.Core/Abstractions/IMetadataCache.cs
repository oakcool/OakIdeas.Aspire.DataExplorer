using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IMetadataCache
{
    Task<DiscoverDatabaseMetadataResponse?> GetAsync(
        string resourceId,
        string databaseName,
        CancellationToken cancellationToken);

    Task SetAsync(
        string resourceId,
        string databaseName,
        DiscoverDatabaseMetadataResponse metadata,
        CancellationToken cancellationToken);

    Task InvalidateAsync(
        string resourceId,
        string databaseName,
        CancellationToken cancellationToken);
}
