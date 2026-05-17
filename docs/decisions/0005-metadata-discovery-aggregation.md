# ADR 0005: Metadata discovery aggregation, cache, and partial-failure strategy

## Status
Accepted

## Context
Metadata discovery needs a single provider-agnostic shape for UI consumption while tolerating partial provider failures and avoiding repeated expensive discovery calls.

## Decision
Use `MetadataAggregationService` to orchestrate discovery in this sequence:

1. Resolve provider via `IProviderFactory`.
2. Perform required schema discovery first.
3. Run optional discovery in async fan-out for object categories and object details.
4. Capture optional failures as `MetadataCollectionFailure` and return `PartialSuccess` when possible.
5. Store and serve metadata snapshots via `IMetadataCache` with configurable TTL.

## Consequences

- UI receives a stable aggregated metadata model.
- Partial failures are visible without fully blocking exploration.
- Cache improves repeated metadata requests during development sessions.
- Aggregation remains provider-agnostic; provider-specific behavior stays isolated.
