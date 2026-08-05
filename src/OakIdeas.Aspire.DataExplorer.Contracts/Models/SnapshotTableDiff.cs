namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Summarizes the difference between the current database state and a restore point for a single table.
/// </summary>
public sealed class SnapshotTableDiff
{
    /// <summary>Schema-qualified table name (e.g. <c>dbo.Orders</c>).</summary>
    public required string TableName { get; init; }

    /// <summary>Row count in the snapshot at the time it was taken.</summary>
    public required long SnapshotRowCount { get; init; }

    /// <summary>Current row count in the live database.</summary>
    public required long CurrentRowCount { get; init; }

    /// <summary>Net row difference: positive means rows were added, negative means rows were deleted.</summary>
    public long RowDelta => CurrentRowCount - SnapshotRowCount;

    /// <summary>Returns <see langword="true"/> when the row count has changed since the snapshot.</summary>
    public bool HasChanged => RowDelta != 0;
}
