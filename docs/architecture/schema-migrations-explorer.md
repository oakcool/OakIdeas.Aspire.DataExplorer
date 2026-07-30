# Schema and Migrations Explorer Design

## Scope and rollout state

Issue: `#126` introduces a development-time Schema and Migrations experience for Aspire-hosted applications.

The initial rollout is protected by the dedicated feature flag:

- **Key**: `Explorer.SchemaMigrations`
- **Default**: `false` (safe rollout default)
- **Owner**: `Explorer`
- **Lifecycle**: `Preview`

When disabled, navigation and direct route usage are blocked in the UI by rendering a feature-unavailable surface.

## Status

All four implementation phases are complete behind the `Explorer.SchemaMigrations` preview flag.

## Phase model

1. **Phase 1: Analysis and routing (complete)**  
   - Centralized feature flag registration, navigation visibility, and direct-route guard.
2. **Phase 2: Migration status (complete)**  
   - Applied, pending, missing-from-project, and out-of-order migrations are projected into shared contracts.
3. **Phase 3: Schema comparison (complete)**  
   - Live schema is compared against the EF Core runtime model, the migrations snapshot, and an optional comparison database.
4. **Phase 4: Script generation and execution (complete)**  
   - Pending, idempotent, and full scripts can be generated, previewed, and executed after explicit database-name confirmation.

## Architecture boundaries

- Shared projects define provider-neutral request/response contracts and orchestration.
- Provider projects own provider SQL, provider-specific migration discovery, EF Core integration, idempotent script support, and exception mapping.
- User-visible failures flow through existing error contracts with sanitized diagnostics.
- Read-only behavior remains default; any state-changing operation requires explicit confirmation.
- AppHost projects can opt into EF Core migration discovery by attaching schema-migrations DbContext metadata to SQL Server database resources.

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
