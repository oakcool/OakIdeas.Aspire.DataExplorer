namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to compare the current database state with a named snapshot.
/// </summary>
public sealed class CompareSnapshotRequest
{
    /// <summary>The ID of the snapshot to compare against.</summary>
    public required string SnapshotId { get; init; }
}
