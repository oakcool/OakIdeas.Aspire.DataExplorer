namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// A single query-level performance record returned by the Query Performance Workspace.
/// Aggregated from provider sources such as SQL Server Query Store.
/// </summary>
public sealed record QueryPerformanceEntry
{
    /// <summary>Provider-assigned query identifier (e.g., query_id from Query Store).</summary>
    public required string QueryId { get; init; }

    /// <summary>Normalized query text with parameters replaced by placeholders.</summary>
    public required string QueryText { get; init; }

    /// <summary>Database name this query was executed against.</summary>
    public required string DatabaseName { get; init; }

    /// <summary>Schema and object name of the query's primary target, when available.</summary>
    public string? ObjectName { get; init; }

    /// <summary>Total number of executions recorded in the collection window.</summary>
    public long ExecutionCount { get; init; }

    /// <summary>Average duration per execution in milliseconds.</summary>
    public double AvgDurationMs { get; init; }

    /// <summary>Maximum single-execution duration in milliseconds.</summary>
    public double MaxDurationMs { get; init; }

    /// <summary>Total cumulative duration across all executions in milliseconds.</summary>
    public double TotalDurationMs { get; init; }

    /// <summary>Average logical reads per execution.</summary>
    public double AvgLogicalReads { get; init; }

    /// <summary>Total logical reads across all executions.</summary>
    public double TotalLogicalReads { get; init; }

    /// <summary>Average logical writes per execution.</summary>
    public double AvgLogicalWrites { get; init; }

    /// <summary>Total logical writes across all executions.</summary>
    public double TotalLogicalWrites { get; init; }

    /// <summary>Number of executions that resulted in an error.</summary>
    public long FailureCount { get; init; }

    /// <summary>Average rows returned per execution.</summary>
    public double AvgRowCount { get; init; }

    /// <summary>When the query was first seen by the provider.</summary>
    public DateTimeOffset? FirstSeenAt { get; init; }

    /// <summary>When the query was most recently executed.</summary>
    public DateTimeOffset? LastSeenAt { get; init; }

    /// <summary>
    /// Whether a performance regression has been detected for this query
    /// (e.g., plan change causing a significant duration increase).
    /// </summary>
    public bool HasRegression { get; init; }

    /// <summary>Number of distinct execution plans recorded for this query.</summary>
    public int PlanCount { get; init; }
}
