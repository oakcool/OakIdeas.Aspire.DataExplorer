# ADR 0015: Relationship-Aware Data Navigator Rollout Flag

## Status

Accepted

## Context

The Relationship-Aware Data Navigator is a multi-phase feature that allows developers to navigate from a record to its parent, child, and many-to-many related records without writing JOIN queries. The feature spans new contracts, provider abstractions, a dedicated service, and a new UI page. Partial implementations must not be reachable by default, and direct URL navigation must respect the rollout state. Backend services, navigation, page guards, and the provider layer must all enforce the flag independently.

## Decision

Introduce a dedicated application feature flag:

- Key: `Navigator.RelationshipAwareNavigator`
- Default: `false`
- Category: `Navigator`
- Lifecycle: `Preview`
- Owner: `Navigator`

The feature flag is enforced at three independent points:

1. **Navigation** — the "Navigator" link is only rendered when `FeatureFlagStateService.RelationshipAwareNavigatorEnabled` is `true`.
2. **Page guard** — `RecordNavigatorPage.razor` renders a "feature disabled" banner and returns immediately if the flag is off; no service calls are made.
3. **Service boundary** — `RelationshipNavigatorService` checks `ApplicationFeatures.RelationshipAwareNavigator` at every public entry point and returns empty results when the flag is disabled.

A SQL Server-specific sub-feature flag (`SqlServer.RelationshipNavigation`) is contributed by `SqlServerFeatureContributor`. It declares a `DependsOn` link to `Navigator.RelationshipAwareNavigator` so that disabling the top-level flag automatically cascades to disable the provider capability.

The `Navigator` area prefix is used for the key to create a dedicated namespace for relationship navigation sub-features (e.g., graph view, delete impact analysis) that may be added in future phases.

A new `Navigator` value is added to `FeatureCategory` to group all navigator-related flags in the Settings / Feature Flags UI.

## Consequences

### Positive

- Existing installations are unaffected when upgrading — the flag defaults to `false`.
- Incomplete phases cannot be reached through UI navigation, direct URL access, or API calls when the flag is disabled.
- The provider-specific sub-feature flag allows SQL Server capability detection to be decoupled from the top-level navigator toggle.
- The flag can be enabled per environment (`appsettings.Development.json`) without requiring code changes.

### Negative

- The flag must be removed in a future cleanup step once the feature is stable and generally available.
- Developers who want to use the feature must explicitly enable it in their local configuration.

## Rollout Plan

1. Default: `false` (Preview — opt-in only).
2. Validate feature stability over two minor releases.
3. Change default to `true` when the feature is generally available.
4. Remove flag after one additional release cycle per the retirement criteria in `docs/architecture/relationship-navigator.md`.
