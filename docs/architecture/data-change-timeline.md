# Data Change Timeline

The Data Change Timeline feature captures and displays inserts, updates, and deletes that occur while a developer exercises an application feature. It is designed as a development workflow tool for Aspire-based applications.

## Status

Implementation phases 1–3 complete. Feature is preview and disabled by default.

## Feature Flag

| Property | Value |
|----------|-------|
| Key | `Timeline.DataChangeTimeline` |
| Category | `Telemetry` |
| Default | `false` (disabled by default) |
| Lifecycle | `Preview` |

The feature is disabled by default so that existing installations retain their current behavior when upgrading. It must be explicitly enabled in configuration.

```json
{
  "OakIdeas": {
    "Aspire": {
      "DataExplorer": {
        "FeatureFlags": {
          "Timeline.DataChangeTimeline": true
        }
      }
    }
  }
}
```

The feature flag is enforced in three independent locations:

1. **Navigation** — The "Timeline" nav link is only rendered when `FeatureFlagStateService.DataChangeTimelineEnabled` returns `true`.
2. **Page guard** — `DataChangeTimelinePage.razor` renders an "unavailable" banner and returns immediately when the flag is off; no service calls are made.
3. **Service registration** — `AddChangeTimelineServices()` is called unconditionally in `Program.cs`, but the service itself stores only in-memory development data and is safe to have registered when unused.

Direct URL navigation to `/data-change-timeline` is handled by the page guard — the feature is inaccessible through any path when disabled.

## Architecture

### Capture Sessions

The feature is built around named capture sessions rather than continuous background capture. A developer starts a session, exercises their application, and then reviews the recorded changes. This keeps overhead predictable and gives the developer control over what is captured.

A session has a simple lifecycle:

```
[not started] → Active → Paused → Active → Stopped
                       ↘                 ↗
                         ─────────────────
```

- Only one session may be active (Active or Paused) at a time. Starting a new session stops any existing active session automatically.
- Events are silently dropped for paused or stopped sessions.
- Stopped sessions retain their captured events for review until explicitly deleted.

### Provider-Neutral Contracts

All session management and event storage contracts live in the shared `Core` and `Contracts` layers:

| Contract / Type | Location | Purpose |
|---|---|---|
| `IChangeTimelineService` | `Core/Abstractions` | Session lifecycle, event recording, querying |
| `DataChangeEvent` | `Contracts/Models` | Single DML change record |
| `DataChangeOperation` | `Contracts/Models` | Insert / Update / Delete enum |
| `ColumnChange` | `Contracts/Models` | Before/after pair for a single column |
| `CaptureSession` | `Contracts/Models` | Session metadata and state |
| `CaptureSessionState` | `Contracts/Models` | Active / Paused / Stopped lifecycle enum |
| `DataChangeQueryRequest` | `Contracts/Models` | Filter parameters for event queries |
| `DataChangeQueryResponse` | `Contracts/Models` | Filtered event results with truncation flag |

### In-Memory Service

`InMemoryChangeTimelineService` is the default implementation of `IChangeTimelineService`. It is:

- **Thread-safe** using a `Lock` for all mutable state.
- **Bounded** per session via `DefaultMaxEventsPerSession` (5,000). Oldest events are evicted when the cap is reached.
- **Development-only** — state is not persisted across restarts.

The service is registered as a singleton via `AddChangeTimelineServices()` from `Core/Extensions/ChangeTimelineServiceCollectionExtensions.cs`.

### Event Model

Each `DataChangeEvent` captures:

| Field | Description |
|---|---|
| `EventId` | Unique identifier assigned by the capturing agent |
| `SessionId` | ID of the owning capture session |
| `Timestamp` | UTC time when the change was recorded |
| `Operation` | Insert, Update, or Delete |
| `DatabaseName` | Target database |
| `SchemaName` | Schema owning the affected table |
| `TableName` | Affected table name |
| `PrimaryKeyColumns` | Ordered list of PK column names |
| `PrimaryKeyValues` | PK column → masked value mapping |
| `Changes` | Column name → `ColumnChange(Before, After)` for modified columns |
| `TraceId` | Optional correlated OpenTelemetry trace ID |
| `TransactionId` | Optional database transaction identifier |

Column values and primary key values **must be masked by the provider** before being passed to `RecordEvent`. Sensitive values (passwords, tokens, configured sensitive fields) must not appear in the event store, logs, or exports.

### Provider Extension Points

The current implementation uses `IChangeTimelineService.RecordEvent` to receive pre-formed `DataChangeEvent` objects. Future provider implementations will feed events into the service through a provider-specific capture mechanism. The expected extension patterns are:

1. **Polling capture** (e.g., SQL Server Change Tracking): A background service polls the provider-specific CDC mechanism and calls `RecordEvent` for each change.
2. **Trigger-based capture**: Provider installs temporary audit triggers and relays events via the service API.
3. **Application-level capture**: Application code calls the service directly via an SDK integration.

Provider-specific capability declarations (whether a provider supports change tracking and which mechanisms are available) should be added to `ProviderCapabilities` when provider implementations are added.

## UI

The `DataChangeTimelinePage.razor` page at `/data-change-timeline` provides:

- **Session controls**: Start, Pause, Resume, Stop buttons.
- **Sessions sidebar**: List of all sessions with state indicator dot (green = Active, amber = Paused, grey = Stopped).
- **Filters**: Table (schema.table), Operation (Insert/Update/Delete).
- **Event list**: Chronological list with operation badge, table name, primary key summary, and trace ID.
- **Event detail**: Primary key values, full column change table (Before/After), event metadata.
- **Export button**: Reserved for Phase 4 JS interop download.
- **Clear button**: Remove all events from the selected session without deleting the session.

## Security and Safety

- Column values and primary key values must be masked before being passed to `RecordEvent`. The service does not perform masking itself.
- The feature is protected by the `Timeline.DataChangeTimeline` feature flag at both the navigation and page levels.
- No connection strings or secrets are stored in change events.
- Events are in-memory and scoped to the development process — they are not persisted.

## Testing

Unit tests for `InMemoryChangeTimelineService` are in `DataChangeTimelineServiceTests.cs` in the `OakIdeas.Aspire.DataExplorer.Core.Tests` project. Coverage includes:

- Session lifecycle (start, pause, resume, stop, delete)
- Event recording (active/paused/stopped/unknown session behavior, eviction at cap)
- Query filtering (table, schema, operation, trace ID, since, max events, truncation)
- `GetTableNames` projection and deduplication
- `ClearEvents` behavior
- `ActiveSession` property correctness
- Error cases and argument validation

The `ApplicationFeaturesCatalogTests` test class validates:
- `DataChangeTimeline` defaults to disabled
- `DataChangeTimeline` key is in the preview/development exclusion set
- `DataChangeTimeline` key appears in `ApplicationFeatures.All`

## Feature Flag Rollout and Retirement

| Stage | Action |
|---|---|
| Preview | Default off; requires explicit opt-in |
| General availability | Change `DefaultEnabled` to `true`; remove from the preview exclusion list in `ApplicationFeaturesCatalogTests` |
| Flag retirement | Remove `FeatureKeys.TimelineDataChangeTimeline`, `ApplicationFeatures.DataChangeTimeline`, the `DataChangeTimelineEnabled` property in `FeatureFlagStateService`, and the feature guard in `DataChangeTimelinePage.razor`; update this document |

Rollback: set `Timeline.DataChangeTimeline: false` in configuration. The feature becomes immediately inaccessible; no database changes or rollback steps are required.
