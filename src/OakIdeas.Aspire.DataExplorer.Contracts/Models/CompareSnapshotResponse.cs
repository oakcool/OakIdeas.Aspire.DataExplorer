namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response from comparing the current database state with a named snapshot.
/// </summary>
public sealed class CompareSnapshotResponse
{
    /// <summary>The snapshot that was used for comparison.</summary>
    public DatabaseSnapshot? Snapshot { get; init; }

    /// <summary>Per-table differences between the snapshot and the current database state.</summary>
    public IReadOnlyList<SnapshotTableDiff> TableDiffs { get; init; } = [];

    /// <summary>Error details when comparison failed.</summary>
    public DataExplorerError? Error { get; init; }

    /// <summary>Returns <see langword="true"/> when the comparison completed successfully.</summary>
    public bool Success => Error is null;

    /// <summary>Returns <see langword="true"/> when at least one table has changed since the snapshot.</summary>
    public bool HasChanges => TableDiffs.Any(d => d.HasChanged);
}
