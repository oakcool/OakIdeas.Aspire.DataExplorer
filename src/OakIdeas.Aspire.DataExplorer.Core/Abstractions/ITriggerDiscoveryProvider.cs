using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface ITriggerDiscoveryProvider
{
    Task<DiscoverTriggersResponse> DiscoverTriggersAsync(
        DatabaseResource resource,
        DiscoverTriggersRequest request,
        CancellationToken cancellationToken);
}
