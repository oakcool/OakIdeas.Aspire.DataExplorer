using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IMetadataRefreshService
{
    Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(
        SelectedDatabaseContext selectedDbContext,
        CancellationToken cancellationToken);

    Task<RefreshMetadataResponse?> GetRefreshStatusAsync(
        CancellationToken cancellationToken);
}

