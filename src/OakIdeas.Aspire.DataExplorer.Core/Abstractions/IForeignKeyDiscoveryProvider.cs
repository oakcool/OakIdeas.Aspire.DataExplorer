using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IForeignKeyDiscoveryProvider
{
    Task<DiscoverForeignKeysResponse> DiscoverForeignKeysAsync(
        DatabaseResource resource,
        DiscoverForeignKeysRequest request,
        CancellationToken cancellationToken);
}
