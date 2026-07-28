using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IViewDiscoveryProvider
{
    Task<DiscoverViewsResponse> DiscoverViewsAsync(
        DatabaseResource resource,
        DiscoverViewsRequest request,
        CancellationToken cancellationToken);
}
