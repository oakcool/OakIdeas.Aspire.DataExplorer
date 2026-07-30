# Relationship-Aware Data Navigator

The Relationship-Aware Data Navigator allows developers to navigate from a record to its parent, child, and many-to-many related records without manually writing JOIN queries. It is designed as a development workflow tool for Aspire-based applications.

## Status

Implementation phases 1–3 complete. Feature is preview and disabled by default.

## Feature Flag

| Property | Value |
|----------|-------|
| Key | `Navigator.RelationshipAwareNavigator` |
| Category | `Navigator` |
| Default | `false` (disabled by default) |
| Lifecycle | `Preview` |

The feature is disabled by default so that existing installations retain their current behavior when upgrading. It must be explicitly enabled in configuration.

```json
{
  "OakIdeas": {
    "Aspire": {
      "DataExplorer": {
        "FeatureFlags": {
          "Navigator.RelationshipAwareNavigator": true
        }
      }
    }
  }
}
```

The feature flag is enforced at three independent locations:

1. **Navigation** — The "Navigator" nav link is only rendered when `FeatureFlagStateService.RelationshipAwareNavigatorEnabled` returns `true`.
2. **Page guard** — `RecordNavigatorPage.razor` renders an "unavailable" banner and returns immediately when the flag is off; no service calls are made.
3. **Service boundary** — `RelationshipNavigatorService` checks the flag at every public entry point and returns empty results when the flag is disabled.

Direct URL navigation to `/record-navigator` is handled by the page guard — the feature is inaccessible through any path when disabled.

### SqlServer Sub-Feature Flag

| Property | Value |
|----------|-------|
| Key | `SqlServer.RelationshipNavigation` |
| Category | `Provider` |
| Default | `true` |
| DependsOn | `Navigator.RelationshipAwareNavigator` |

The SQL Server provider contributes a capability-specific flag that declares its dependency on the top-level navigator flag. Disabling `Navigator.RelationshipAwareNavigator` automatically cascades to disable `SqlServer.RelationshipNavigation`.

## Architecture

### Phase 1: Relationship Metadata

Foreign key relationships are already discovered by the existing `IForeignKeyDiscoveryProvider` infrastructure. The navigator adds a new abstraction (`IRelationshipNavigationProvider`) that projects FK metadata into navigation-oriented `TableRelationship` records.

Each `TableRelationship` captures:
- The **kind** of relationship from the current table's perspective: `Parent` (current table holds the FK), `Child` (current table is referenced), or `ManyToMany`.
- The **related table** (schema + name).
- **Column mappings** from source column to related column.
- Whether the constraint is **enforced**.

Self-referencing relationships (e.g., `Employee.ManagerId → Employee.Id`) produce two entries — one for each navigation direction.

### Phase 2: Record Navigation

Once relationships are discovered, the developer selects a relationship and enters key values. The navigator:

1. **Counts** related records without loading them (via `GetRelatedRecordCountAsync`).
2. **Navigates** to load the related records with pagination (via `NavigateRelatedRecordsAsync`).
3. **Generates** the underlying T-SQL query and displays it for copy/reuse.

Queries are always **parameterized** — user-entered values are never interpolated into SQL strings. Identifier names are bracket-quoted and `]` characters within identifiers are escaped with `]]`.

### Phase 3: UI

The navigator page (`/record-navigator`) provides:

- A schema and table input to select the target table.
- Relationship cards with colored badges (blue for parent, green for child).
- Per-relationship key value inputs.
- Count preview button.
- Navigate button to load related rows.
- Generated SQL displayed below each result set.
- Reset button to clear the state.

## Provider Contract

```csharp
public interface IRelationshipNavigationProvider
{
    DatabaseProviderType ProviderType { get; }

    Task<DiscoverTableRelationshipsResponse> DiscoverTableRelationshipsAsync(
        DatabaseResource resource,
        DiscoverTableRelationshipsRequest request,
        CancellationToken cancellationToken);

    Task<GetRelatedRecordCountResponse> GetRelatedRecordCountAsync(
        DatabaseResource resource,
        GetRelatedRecordCountRequest request,
        CancellationToken cancellationToken);

    Task<NavigateRelatedRecordsResponse> NavigateRelatedRecordsAsync(
        DatabaseResource resource,
        NavigateRelatedRecordsRequest request,
        CancellationToken cancellationToken);
}
```

Provider implementations are registered as singletons per provider type and resolved by `RelationshipNavigatorService` based on the currently selected database's provider type.

## SQL Server Implementation

`SqlServerRelationshipNavigationProvider` queries `sys.foreign_keys`, `sys.foreign_key_columns`, `sys.tables`, `sys.schemas`, and `sys.columns` to discover FK relationships. It returns both parent and child perspectives for each constraint involving the target table.

Navigation queries use `OFFSET/FETCH NEXT` for pagination and are always parameterized.

## Security

- All column values entered by the developer are passed as SQL parameters — never interpolated.
- Identifier names (schema, table, column) are bracket-quoted with `]` escaped as `]]`.
- The generated SQL shown in the UI is informational only — it is the exact parameterized SQL statement that was executed, with no secret values embedded.
- Connection strings are never exposed in UI, logs, or telemetry.

## Rollout Guidance

1. Enable the flag in `appsettings.Development.json` to validate the feature.
2. Verify that the nav link appears and the page loads correctly for SQL Server databases.
3. Verify that disabling the flag hides the nav link and shows the "unavailable" message on direct URL access.
4. Promote to `DefaultEnabled = true` once the feature is stable and generally available.

## Retirement Criteria

The feature flag `Navigator.RelationshipAwareNavigator` should be removed when:

1. The feature has been stable in production for at least two minor releases.
2. No critical defects or edge cases are outstanding.
3. The flag has been set to `DefaultEnabled = true` for at least one release cycle.

When retiring, remove the flag constant from `FeatureKeys`, the feature definition from `ApplicationFeatures`, the `RelationshipAwareNavigatorEnabled` property from `FeatureFlagStateService`, and all `@if (FeatureFlags.RelationshipAwareNavigatorEnabled)` guards from the UI. The `SqlServer.RelationshipNavigation` sub-feature flag should also be retired at the same time.
