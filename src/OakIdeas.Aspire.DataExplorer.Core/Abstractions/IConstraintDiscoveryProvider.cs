using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IConstraintDiscoveryProvider
{
    Task<DiscoverConstraintsResponse> DiscoverConstraintsAsync(
        DatabaseResource resource,
        DiscoverConstraintsRequest request,
        CancellationToken cancellationToken);
}
