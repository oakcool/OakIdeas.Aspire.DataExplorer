namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to rename an existing snapshot.
/// </summary>
public sealed class RenameSnapshotRequest
{
    /// <summary>The ID of the snapshot to rename.</summary>
    public required string SnapshotId { get; init; }

    /// <summary>The new name for the snapshot.</summary>
    public required string NewName { get; init; }

    /// <summary>Optional updated notes.</summary>
    public string? Notes { get; init; }
}
