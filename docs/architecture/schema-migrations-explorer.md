# Schema and Migrations Explorer Design

## Scope and rollout state

Issue: `#126` introduces a development-time Schema and Migrations experience for Aspire-hosted applications.

The initial rollout is protected by the dedicated feature flag:

- **Key**: `Explorer.SchemaMigrations`
- **Default**: `false` (safe rollout default)
- **Owner**: `Explorer`
- **Lifecycle**: `Preview`

When disabled, navigation and direct route usage are blocked in the UI by rendering a feature-unavailable surface.

## Phase model

The feature is delivered in phases:

1. **Phase 1 (this baseline)**  
   - Register the feature flag in the centralized catalog.
   - Add a dedicated route and navigation entry behind the flag.
   - Keep the page read-only and non-operational while backend comparison/execution services are not implemented.
2. **Phase 2**  
   - Add applied vs pending migration status using provider-capability-aware contracts.
3. **Phase 3**  
   - Add schema comparison (live schema, model, snapshot) with severity categorization.
4. **Phase 4**  
   - Add script generation and explicitly confirmed execution controls.

## Architecture boundaries

- Shared projects define provider-neutral request/response contracts and orchestration.
- Provider projects own provider SQL, provider-specific migration discovery, idempotent script support, and exception mapping.
- User-visible failures flow through existing error contracts with sanitized diagnostics.
- Read-only behavior remains default; any state-changing operation requires explicit confirmation.

## Feature flag enforcement

- Feature availability is centralized via `IFeatureFlagService` and `FeatureFlagStateService`.
- The page enforces the flag server-side on initialization and render path.
- Navigation visibility is also flag-aware.
- Direct URL access cannot bypass the flag because the page itself checks and blocks rendering of active functionality.

## Configuration sources and precedence

The flag uses existing feature flag source providers and precedence rules documented in `feature-flags.md`:

1. Higher-priority providers (future emergency override / remote / database)
2. Configuration source
3. Catalog default

## Telemetry and retirement guidance

- Track rollout by recording whether `Explorer.SchemaMigrations` evaluated to enabled/disabled.
- Keep the feature flag until the full implementation reaches generally available status.
- Retirement criteria:
  - all phase features complete,
  - tests cover enabled/disabled/missing/invalid/runtime-change states,
  - rollback guidance no longer depends on the flag.
