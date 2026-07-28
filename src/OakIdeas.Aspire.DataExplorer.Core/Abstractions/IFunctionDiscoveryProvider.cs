using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IFunctionDiscoveryProvider
{
    Task<DiscoverFunctionsResponse> DiscoverFunctionsAsync(
        DatabaseResource resource,
        DiscoverFunctionsRequest request,
        CancellationToken cancellationToken);
}
