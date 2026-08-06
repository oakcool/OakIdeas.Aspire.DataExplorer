namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response from a query performance ranking request.
/// </summary>
public sealed record GetQueryPerformanceResponse
{
    /// <summary>Ranked query entries matching the request filters.</summary>
    public required IReadOnlyList<QueryPerformanceEntry> Entries { get; init; }

    /// <summary>Total number of entries available before the <c>Limit</c> was applied.</summary>
    public int TotalCount { get; init; }

    /// <summary>Whether the provider supports query performance data for the requested database.</summary>
    public bool IsSupported { get; init; }

    /// <summary>Human-readable message explaining why results are unavailable, when <see cref="IsSupported"/> is <see langword="false"/>.</summary>
    public string? UnsupportedReason { get; init; }

    /// <summary>Data source used to populate the entries (e.g., "SQL Server Query Store").</summary>
    public string? DataSource { get; init; }
}
