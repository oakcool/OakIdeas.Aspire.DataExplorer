using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IAspireResourceDiscovery
{
    Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
        DiscoverResourcesRequest request,
        CancellationToken cancellationToken);
}

