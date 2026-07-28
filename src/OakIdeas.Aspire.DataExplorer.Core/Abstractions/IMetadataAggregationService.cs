using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IMetadataAggregationService
{
    Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
        SelectedDatabaseContext selectedDbContext,
        CancellationToken cancellationToken);
}

