using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface ISchemaDiscoveryProvider
{
    Task<DiscoverSchemasResponse> DiscoverSchemasAsync(
        DatabaseResource resource,
        DiscoverSchemasRequest request,
        CancellationToken cancellationToken);
}

