namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request for querying the performance workspace ranking.
/// </summary>
public sealed record GetQueryPerformanceRequest
{
    /// <summary>Optional database name filter. When <see langword="null"/> or empty, all databases are included.</summary>
    public string? DatabaseName { get; init; }

    /// <summary>Optional free-text filter applied to <see cref="QueryPerformanceEntry.QueryText"/>.</summary>
    public string? QueryTextFilter { get; init; }

    /// <summary>When <see langword="true"/>, only queries with detected regressions are returned.</summary>
    public bool RegressionsOnly { get; init; }

    /// <summary>Field to sort by. Defaults to <see cref="QueryPerformanceSortField.AvgDuration"/>.</summary>
    public QueryPerformanceSortField SortBy { get; init; } = QueryPerformanceSortField.AvgDuration;

    /// <summary>Maximum number of entries to return. Defaults to 50.</summary>
    public int Limit { get; init; } = 50;
}
