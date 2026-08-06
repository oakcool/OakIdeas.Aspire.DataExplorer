namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to restore the database to a named snapshot state.
/// </summary>
public sealed class RestoreSnapshotRequest
{
    /// <summary>The ID of the snapshot to restore from.</summary>
    public required string SnapshotId { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the restore is simulated (dry run) and no data is changed.
    /// The response will describe what would happen.
    /// </summary>
    public bool DryRun { get; init; }
}
