namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response from a snapshot restore operation.
/// </summary>
public sealed class RestoreSnapshotResponse
{
    /// <summary>The snapshot that was used for the restore.</summary>
    public DatabaseSnapshot? Snapshot { get; init; }

    /// <summary>
    /// When <see langword="true"/>, this was a dry run and no data was actually changed.
    /// </summary>
    public bool WasDryRun { get; init; }

    /// <summary>
    /// Human-readable summary describing the tables that were restored and their row counts.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>Error details when the restore failed.</summary>
    public DataExplorerError? Error { get; init; }

    /// <summary>Returns <see langword="true"/> when the restore completed (or dry run succeeded) without error.</summary>
    public bool Success => Error is null;
}
