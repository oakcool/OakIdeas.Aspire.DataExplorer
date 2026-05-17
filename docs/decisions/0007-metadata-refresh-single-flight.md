# ADR 0007: Metadata refresh single-flight and cache invalidation

## Status
Accepted

## Context
Users can trigger metadata refresh repeatedly from the UI. Concurrent refresh operations can duplicate provider calls, create race conditions, and produce confusing status outcomes.

## Decision
`MetadataRefreshService` enforces a single-flight refresh model:

- Acquire a non-blocking `SemaphoreSlim` lock per service instance.
- If lock is not available, return `RefreshStatus.InProgress` immediately.
- Invalidate cache first, then run aggregation, then write fresh metadata snapshot.
- Persist latest refresh status for retrieval via `GetRefreshStatusAsync`.

## Consequences

- Refresh behavior is deterministic and resilient to repeated UI clicks.
- Cache state remains coherent after refresh.
- Refresh API communicates in-progress/cancelled/failed/completed outcomes explicitly.
