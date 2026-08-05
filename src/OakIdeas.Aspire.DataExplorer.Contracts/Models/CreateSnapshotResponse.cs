namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response from creating a database snapshot.
/// </summary>
public sealed class CreateSnapshotResponse
{
    /// <summary>The created snapshot, or <see langword="null"/> when creation failed.</summary>
    public DatabaseSnapshot? Snapshot { get; init; }

    /// <summary>Error details when creation failed.</summary>
    public DataExplorerError? Error { get; init; }

    /// <summary>Returns <see langword="true"/> when the snapshot was created successfully.</summary>
    public bool Success => Snapshot is not null && Error is null;
}
