using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IColumnDiscoveryProvider
{
    Task<DiscoverColumnsResponse> DiscoverColumnsAsync(
        DatabaseResource resource,
        DiscoverColumnsRequest request,
        CancellationToken cancellationToken);
}
