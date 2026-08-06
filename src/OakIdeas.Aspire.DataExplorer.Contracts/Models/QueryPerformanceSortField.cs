namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Sort field options for the Query Performance Workspace ranking view.
/// </summary>
public enum QueryPerformanceSortField
{
    /// <summary>Sort by average duration (slowest first).</summary>
    AvgDuration = 0,

    /// <summary>Sort by total cumulative duration (most expensive first).</summary>
    TotalDuration = 1,

    /// <summary>Sort by execution count (most frequent first).</summary>
    ExecutionCount = 2,

    /// <summary>Sort by total logical reads (highest I/O first).</summary>
    TotalLogicalReads = 3,

    /// <summary>Sort by failure count (most failures first).</summary>
    FailureCount = 4,

    /// <summary>Sort by most recently executed (latest first).</summary>
    LastSeenAt = 5,
}
