using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Provides query performance data for the Query Performance Workspace.
/// Implementations are provider-specific and may read from SQL Server Query Store
/// or equivalent sources. A no-op implementation is used when the provider does not
/// support performance metrics.
/// </summary>
public interface IQueryPerformanceService
{
    /// <summary>
    /// Returns ranked query performance entries matching the specified request filters.
    /// </summary>
    /// <param name="request">Ranking and filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GetQueryPerformanceResponse> GetTopQueriesAsync(
        GetQueryPerformanceRequest request,
        CancellationToken cancellationToken = default);
}
