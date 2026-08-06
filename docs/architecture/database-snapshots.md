# Database Snapshots

## Overview

The Database Snapshots feature allows developers to capture a named restore point before testing a feature, compare the current database state with a restore point, and restore the database to a known-good state during development.

This feature is designed as part of the development workflow for Aspire-based applications and integrates with the existing provider model, feature flag architecture, and UI design system.

## Feature Flag

| Property | Value |
|---|---|
| **Flag key** | `Snapshots.DatabaseSnapshots` |
| **Default** | `false` (disabled) |
| **Lifecycle** | Preview |
| **Owner** | Snapshots |

The feature is disabled by default for safe rollout. Enable it through any supported feature flag source:

```json
// appsettings.Development.json
{
  "OakIdeas": {
    "Aspire": {
      "DataExplorer": {
        "Features": {
          "Snapshots.DatabaseSnapshots": true
        }
      }
    }
  }
}
```

When the flag is disabled:
- The navigation link is hidden from the sidebar
- The `/snapshots` route shows an "unavailable" message
- No snapshot service operations are visible to users

## Architecture

### Contracts (`OakIdeas.Aspire.DataExplorer.Contracts`)

The following request/response models define the contract:

| Model | Purpose |
|---|---|
| `DatabaseSnapshot` | Represents a named restore point |
| `SnapshotState` | Lifecycle state: Available, Creating, Restoring, Failed |
| `CreateSnapshotRequest` / `CreateSnapshotResponse` | Create a new snapshot |
| `RenameSnapshotRequest` | Rename or update notes for an existing snapshot |
| `DeleteSnapshotRequest` | Delete a snapshot by ID |
| `CompareSnapshotRequest` / `CompareSnapshotResponse` | Compare current state with a snapshot |
| `SnapshotTableDiff` | Per-table row-count difference between snapshot and live state |
| `RestoreSnapshotRequest` / `RestoreSnapshotResponse` | Restore the database to a snapshot state |

`FeatureCategory.Snapshots` was added to the feature category enum.

### Core Abstractions

`ISnapshotService` (in `OakIdeas.Aspire.DataExplorer.Core.Abstractions`) defines the snapshot management contract:

- `GetSnapshots(databaseName)` — list snapshots for a database
- `GetAllSnapshots()` — list all snapshots across databases
- `GetSnapshot(snapshotId)` — look up a specific snapshot
- `CreateSnapshotAsync(request)` — create a new snapshot
- `RenameSnapshot(request)` — rename or update notes
- `DeleteSnapshot(request)` — delete a snapshot
- `CompareSnapshotAsync(request)` — compare current state with snapshot
- `RestoreSnapshotAsync(request)` — restore to snapshot state (with dry-run support)
- `TotalSnapshotCount` — total count across all databases

### Core Services

`InMemorySnapshotService` (in `OakIdeas.Aspire.DataExplorer.Core.Services`) provides a thread-safe, bounded in-memory implementation:

- State is not persisted across restarts (development-time only)
- Compare returns an empty diff (no live DB access in the logical snapshot implementation)
- Restore returns a summary describing what would happen; full provider-native restore is reserved for a future provider-specific implementation
- Dry-run restore is supported and returns a description without any data changes

### Registration

Call `AddSnapshotServices()` from the application composition root:

```csharp
builder.Services.AddSnapshotServices();
```

This registers `ISnapshotService` as a singleton backed by `InMemorySnapshotService`.

### Feature Flag State

`FeatureFlagStateService.DatabaseSnapshotsEnabled` provides the circuit-scoped flag evaluation for UI consumers.

## UI

The snapshot management page is at `/snapshots`.

**When disabled:** shows a standard "feature unavailable" message.

**When enabled:**

- Header with database context and "New Snapshot" button
- Empty state when no snapshots exist
- Snapshot list with name, notes, creation timestamp, and state badge
- Per-snapshot actions: Compare, Restore, Delete
- Comparison result panel with per-table diff table
- Restore result panel with success/dry-run summary
- Confirmation dialog for destructive restore operations

The nav link is shown in the sidebar only when the flag is enabled.

## Security

- No sensitive data (connection strings, credentials) is exposed in the UI or logs
- Restore operations require explicit user confirmation
- Dry-run mode is offered before executing a real restore
- The in-memory implementation does not store actual database content

## Rollout Guidance

1. Deploy with `Snapshots.DatabaseSnapshots: false` (the default)
2. Enable for selected teams/environments via configuration
3. Gather feedback on the in-memory snapshot workflow
4. Implement provider-native snapshot/restore in provider packages before general availability
5. Set `DefaultEnabled = true` and remove the flag when the feature is stable and fully supported across providers

## Future Work

- Provider-native snapshot support (e.g., SQL Server database snapshots)
- Real row-count and schema comparison via live provider queries
- Export snapshot metadata for sharing between developers
- Scoped restore (restore selected tables only)
