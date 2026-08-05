using System.Collections.Concurrent;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ISnapshotService"/>.
/// Stores logical snapshots in memory; no data is persisted across restarts.
/// Intended for development-time use only.
/// Compare and restore operations are no-ops against logical (row-count-only) snapshots.
/// </summary>
public sealed class InMemorySnapshotService : ISnapshotService
{
    private readonly Lock _lock = new();
    private readonly LinkedList<DatabaseSnapshot> _snapshots = new();

    /// <inheritdoc />
    public IReadOnlyList<DatabaseSnapshot> GetSnapshots(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        lock (_lock)
        {
            return _snapshots
                .Where(s => string.Equals(s.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DatabaseSnapshot> GetAllSnapshots()
    {
        lock (_lock) { return [.. _snapshots]; }
    }

    /// <inheritdoc />
    public DatabaseSnapshot? GetSnapshot(string snapshotId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);
        lock (_lock)
        {
            return _snapshots.FirstOrDefault(s => string.Equals(s.Id, snapshotId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public Task<CreateSnapshotResponse> CreateSnapshotAsync(
        CreateSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var snapshot = new DatabaseSnapshot
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name.Trim(),
            Notes = request.Notes?.Trim(),
            DatabaseName = request.DatabaseName,
            ProviderType = request.ProviderType,
            CreatedAt = DateTimeOffset.UtcNow,
            State = SnapshotState.Available,
        };

        lock (_lock)
        {
            _snapshots.AddFirst(snapshot);
        }

        return Task.FromResult(new CreateSnapshotResponse { Snapshot = snapshot });
    }

    /// <inheritdoc />
    public void RenameSnapshot(RenameSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SnapshotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NewName);

        lock (_lock)
        {
            var node = FindNode(request.SnapshotId);
            if (node is null)
            {
                throw new InvalidOperationException($"Snapshot '{request.SnapshotId}' does not exist.");
            }

            var updated = new DatabaseSnapshot
            {
                Id = node.Value.Id,
                Name = request.NewName.Trim(),
                Notes = request.Notes?.Trim() ?? node.Value.Notes,
                DatabaseName = node.Value.DatabaseName,
                ProviderType = node.Value.ProviderType,
                CreatedAt = node.Value.CreatedAt,
                SizeBytes = node.Value.SizeBytes,
                State = node.Value.State,
                ErrorMessage = node.Value.ErrorMessage,
            };

            node.Value = updated;
        }
    }

    /// <inheritdoc />
    public void DeleteSnapshot(DeleteSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SnapshotId);

        lock (_lock)
        {
            var node = FindNode(request.SnapshotId);
            if (node is not null)
            {
                _snapshots.Remove(node);
            }
        }
    }

    /// <inheritdoc />
    public Task<CompareSnapshotResponse> CompareSnapshotAsync(
        CompareSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SnapshotId);

        DatabaseSnapshot? snapshot;
        lock (_lock)
        {
            snapshot = _snapshots.FirstOrDefault(s =>
                string.Equals(s.Id, request.SnapshotId, StringComparison.OrdinalIgnoreCase));
        }

        if (snapshot is null)
        {
            var error = new DataExplorerError(
                Category: ErrorCategory.ResourceNotFound,
                Message: $"Snapshot '{request.SnapshotId}' was not found.",
                RecoverySuggestion: "Verify the snapshot ID and try again.",
                Operation: "CompareSnapshot",
                Target: request.SnapshotId,
                Timestamp: DateTimeOffset.UtcNow);

            return Task.FromResult(new CompareSnapshotResponse { Error = error });
        }

        // In-memory logical snapshot: comparison returns an empty diff (no real data access).
        var response = new CompareSnapshotResponse
        {
            Snapshot = snapshot,
            TableDiffs = [],
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public Task<RestoreSnapshotResponse> RestoreSnapshotAsync(
        RestoreSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SnapshotId);

        DatabaseSnapshot? snapshot;
        lock (_lock)
        {
            snapshot = _snapshots.FirstOrDefault(s =>
                string.Equals(s.Id, request.SnapshotId, StringComparison.OrdinalIgnoreCase));
        }

        if (snapshot is null)
        {
            var error = new DataExplorerError(
                Category: ErrorCategory.ResourceNotFound,
                Message: $"Snapshot '{request.SnapshotId}' was not found.",
                RecoverySuggestion: "Verify the snapshot ID and try again.",
                Operation: "RestoreSnapshot",
                Target: request.SnapshotId,
                Timestamp: DateTimeOffset.UtcNow);

            return Task.FromResult(new RestoreSnapshotResponse { Error = error });
        }

        // In-memory logical snapshot: restore is a no-op (provider-specific restore is not supported).
        var response = new RestoreSnapshotResponse
        {
            Snapshot = snapshot,
            WasDryRun = request.DryRun,
            Summary = request.DryRun
                ? $"Dry run: restore from snapshot '{snapshot.Name}' ({snapshot.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC) would reset the database to its captured state."
                : $"Restore from snapshot '{snapshot.Name}' ({snapshot.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC) is not supported for logical (in-memory) snapshots. Use a provider-native backup for full restore.",
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public int TotalSnapshotCount
    {
        get
        {
            lock (_lock) { return _snapshots.Count; }
        }
    }

    private LinkedListNode<DatabaseSnapshot>? FindNode(string snapshotId)
    {
        var node = _snapshots.First;
        while (node is not null)
        {
            if (string.Equals(node.Value.Id, snapshotId, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            node = node.Next;
        }

        return null;
    }
}
