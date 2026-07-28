# ADR 0012: Schema and Migrations Rollout Flag

## Status

Accepted

## Context

Schema and migration comparison is a cross-cutting feature that will be delivered incrementally. Partial implementations must not be reachable by default, and direct route access must respect rollout state.

## Decision

Introduce a dedicated application feature flag:

- Key: `Explorer.SchemaMigrations`
- Default: `false`
- Category: `Explorer`
- Lifecycle: `Preview`

The feature flag is enforced in both navigation visibility and page rendering paths, with route-level behavior remaining read-only while implementation is incomplete.

## Consequences

- Existing installations keep current behavior unless explicitly enabled.
- Incomplete phase functionality remains inaccessible by default.
- Future phases can be merged behind the same flag and promoted safely to GA later.
