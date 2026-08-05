using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Manages database snapshots (restore points) for the development-time Database Snapshots feature.
/// Implementations must be thread-safe; the service is registered as a singleton.
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// Returns all snapshots for the specified database in reverse creation order (most recent first).
    /// Returns an empty list when no snapshots exist for the database.
    /// </summary>
    IReadOnlyList<DatabaseSnapshot> GetSnapshots(string databaseName);

    /// <summary>
    /// Returns all snapshots across all databases in reverse creation order (most recent first).
    /// </summary>
    IReadOnlyList<DatabaseSnapshot> GetAllSnapshots();

    /// <summary>
    /// Returns the snapshot with the specified ID, or <see langword="null"/> when not found.
    /// </summary>
    DatabaseSnapshot? GetSnapshot(string snapshotId);

    /// <summary>
    /// Creates a new named snapshot (logical restore point) for the specified database.
    /// </summary>
    /// <param name="request">The snapshot creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateSnapshotResponse> CreateSnapshotAsync(
        CreateSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames an existing snapshot and optionally updates its notes.
    /// </summary>
    /// <param name="request">The rename request.</param>
    /// <exception cref="InvalidOperationException">Thrown when the snapshot does not exist.</exception>
    void RenameSnapshot(RenameSnapshotRequest request);

    /// <summary>
    /// Deletes a snapshot by ID. Silently succeeds when the snapshot does not exist.
    /// </summary>
    /// <param name="request">The delete request.</param>
    void DeleteSnapshot(DeleteSnapshotRequest request);

    /// <summary>
    /// Compares the current database state with the named snapshot and returns per-table row-count differences.
    /// </summary>
    /// <param name="request">The comparison request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The comparison result.</returns>
    Task<CompareSnapshotResponse> CompareSnapshotAsync(
        CompareSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the database to the state captured in the named snapshot.
    /// Supports a dry-run mode that describes what would happen without making changes.
    /// </summary>
    /// <param name="request">The restore request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restore result.</returns>
    Task<RestoreSnapshotResponse> RestoreSnapshotAsync(
        RestoreSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of snapshots across all databases.
    /// </summary>
    int TotalSnapshotCount { get; }
}
