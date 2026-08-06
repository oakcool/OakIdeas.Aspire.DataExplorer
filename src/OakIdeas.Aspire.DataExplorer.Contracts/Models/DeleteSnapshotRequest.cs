namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to delete a snapshot by ID.
/// </summary>
public sealed class DeleteSnapshotRequest
{
    /// <summary>The ID of the snapshot to delete.</summary>
    public required string SnapshotId { get; init; }
}
