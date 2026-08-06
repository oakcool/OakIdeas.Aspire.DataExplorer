using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// No-op implementation of <see cref="IQueryPerformanceService"/> used when no provider-specific
/// implementation is available. Returns an unsupported response so the UI can display a clear
/// capability message rather than an error.
/// </summary>
public sealed class InMemoryQueryPerformanceService : IQueryPerformanceService
{
    /// <inheritdoc />
    public Task<GetQueryPerformanceResponse> GetTopQueriesAsync(
        GetQueryPerformanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new GetQueryPerformanceResponse
        {
            Entries = [],
            TotalCount = 0,
            IsSupported = false,
            UnsupportedReason = "Query performance data is not available for the current database provider. Connect to a SQL Server database with Query Store enabled to use this feature.",
            DataSource = null,
        };

        return Task.FromResult(response);
    }
}
