using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IPrimaryKeyDiscoveryProvider
{
    Task<DiscoverPrimaryKeysResponse> DiscoverPrimaryKeysAsync(
        DatabaseResource resource,
        DiscoverPrimaryKeysRequest request,
        CancellationToken cancellationToken);
}
