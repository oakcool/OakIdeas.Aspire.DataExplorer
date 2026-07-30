# ADR 0014: Data Change Timeline Rollout Flag

## Status

Accepted

## Context

Data Change Timeline is a multi-phase feature that captures and displays inserts, updates, and deletes during a development session. Partial implementations must not be reachable by default, and direct URL navigation must respect the rollout state. Backend services, navigation, page guards, and session management must all enforce the flag independently. The feature involves in-memory mutable state (capture sessions, event stores) that should not be active unless a developer has explicitly opted in.

## Decision

Introduce a dedicated application feature flag:

- Key: `Timeline.DataChangeTimeline`
- Default: `false`
- Category: `Telemetry`
- Lifecycle: `Preview`
- Owner: `Timeline`

The feature flag is enforced at three independent points:

1. **Navigation** — the "Timeline" link is only rendered when `FeatureFlagStateService.DataChangeTimelineEnabled` is `true`.
2. **Page guard** — `DataChangeTimelinePage.razor` renders a "feature disabled" banner and returns immediately if the flag is off; no service or session calls are made.
3. **Service registration** — the `IChangeTimelineService` singleton is registered unconditionally but operates safely as a no-op when no sessions are started. This avoids conditional registration complexity and keeps the service available for future out-of-band capture providers.

The `Timeline` area prefix is used for the key (rather than `Telemetry`) to distinguish change-capture concerns from trace/span concerns and to leave room for additional timeline sub-features (e.g., timeline replay, timeline export) under the same area prefix.

## Consequences

- Existing installations retain current behavior when upgrading; the feature is inaccessible until explicitly enabled.
- The page is unreachable via direct URL, saved links, or stale client state when the flag is off.
- The `Timeline` feature key area is established for future sub-features under the same umbrella.
- When the feature reaches general availability, `DefaultEnabled` is set to `true`, the preview exclusion in `ApplicationFeaturesCatalogTests` is removed, and this ADR is updated.
- The flag can be retired after a defined stabilization period by removing the key constant, catalog entry, `FeatureFlagStateService` property, and page guard.
