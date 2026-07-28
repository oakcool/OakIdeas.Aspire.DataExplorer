using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IStoredProcedureDiscoveryProvider
{
    Task<DiscoverStoredProceduresResponse> DiscoverStoredProceduresAsync(
        DatabaseResource resource,
        DiscoverStoredProceduresRequest request,
        CancellationToken cancellationToken);
}
