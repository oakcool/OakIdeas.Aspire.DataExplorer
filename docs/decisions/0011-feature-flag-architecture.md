# ADR 0011: Provider-Driven Feature Flag Architecture

## Status

Accepted

## Context

The application needs a feature flag system that:

- Can enable or disable capabilities based on configurable sources.
- Integrates with the existing provider architecture and configuration patterns.
- Preserves current behavior by defaulting all existing features to enabled.
- Is extensible to support future database-backed, remote, or per-environment sources.
- Enforces flags bottom-up: at the service layer before the UI layer.

## Decision

Introduce a layered feature flag system with clearly separated responsibilities:

### 1. Feature Catalog (Contracts + Core)

A centralized, strongly typed catalog of feature definitions. Each `FeatureFlag` carries a stable key, display name, description, category, default enabled state, and lifecycle. The catalog is registered once and never modified at runtime.

Feature keys use the pattern `<Area>.<Capability>`, e.g., `Explorer.ObjectExplorer`, `Query.Editor`. All existing features default to `true`.

### 2. Evaluation Abstraction (Core)

`IFeatureFlagService` evaluates a `FeatureFlag` for a given `FeatureEvaluationContext`. The result is a `FeatureFlagResult` containing the effective Boolean value, the source that produced it, an evaluation trace, and whether the catalog default was used.

A convenience method `IsEnabledAsync` is provided. The richer result type is available when diagnostics are needed.

### 3. Source Provider Abstraction (Core)

`IFeatureFlagSourceProvider` defines how individual sources supply flag values. Sources return a `FeatureFlagSourceResult` that distinguishes among:

- Defined and enabled.
- Defined and disabled.
- Not defined (this source has no opinion).
- Source unavailable or error.

Not-defined differs from disabled. A source that has no record of a flag does not block the fallback chain.

### 4. Source Precedence

Sources are ordered by an explicit integer priority (lower = higher precedence). The pipeline walks sources in priority order and stops at the first source that returns a definitive answer (enabled or disabled). If no source defines the flag, the catalog default is used.

Built-in priority bands:
- 0–99: Reserved for emergency or runtime overrides.
- 100–199: Reserved for database or remote sources.
- 200–299: Application configuration source (default: 200).
- `int.MaxValue`: Catalog default (always last).

Duplicate priorities are rejected at startup (Options validation).

### 5. Configuration Source (Phase 3)

The configuration source reads from the standard .NET `IConfiguration` under the section `OakIdeas:Aspire:DataExplorer:FeatureFlags`. Values are Boolean strings. Unknown or invalid values produce a diagnostic and are treated as not-defined.

### 6. Failure Behavior

When a source throws or returns an error, the pipeline logs the failure, records it in the result trace, and continues to the next source. If all sources fail and the catalog has a default, the default is used. This is `UseCatalogDefault` behavior.

The failure mode is configurable via `FeatureFlagOptions.DefaultFailureBehavior`.

### 7. Separation from Provider Capabilities and Authorization

- A **feature flag** answers whether the application intends to expose a capability.
- A **provider capability** answers whether the active provider can perform it.
- **Authorization** answers whether the current user is permitted.
- **Validation** answers whether the request is valid.

These are independent concerns. Feature flags must not replace provider capability checks or authorization.

### 8. Internal Abstraction vs .NET Feature Management

The application uses an internal abstraction (`IFeatureFlagService`) rather than directly consuming `Microsoft.FeatureManagement`. Reasons:

- The existing pattern uses internal service abstractions.
- The application needs richer result types (source attribution, trace, lifecycle).
- `Microsoft.FeatureManagement` can be added as a source provider in a future phase without breaking the internal contract.

### 9. Caching

The configuration source is effectively cached by `IConfiguration` (no additional caching needed in Phase 3). Database or remote sources will define their own TTL and refresh in their respective phases.

### 10. Request-Scoped Consistency

In Phase 3 the configuration source is stateless, so snapshot consistency is not required. Future phases with database or remote sources will introduce operation-scoped snapshots.

## Consequences

- All existing features remain enabled by default.
- No production behavior changes in Phase 2 or Phase 3.
- The abstraction is open for database-backed, remote, and emergency-override sources.
- Feature flags are cleanly separated from provider capabilities and authorization.
- Duplicate source priority is caught at startup, not at evaluation time.
