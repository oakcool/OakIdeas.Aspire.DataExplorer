using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface ITableDiscoveryProvider
{
    Task<DiscoverTablesResponse> DiscoverTablesAsync(
        DatabaseResource resource,
        DiscoverTablesRequest request,
        CancellationToken cancellationToken);
}

