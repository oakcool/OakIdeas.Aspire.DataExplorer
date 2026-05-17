# Architecture Review — 2026-05

This review captures the current state of the solution, highlights the most important implementation gaps observed during the review, and records the next recommended steps.

## Current assessment

### Strengths

- Provider isolation is clear: `Core` orchestrates contracts and workflows, while SQL Server-specific behavior stays in `SqlServer`.
- Development-only guardrails are enforced in both hosting and web startup paths.
- Metadata discovery already uses explicit request/response contracts and partial-failure handling.
- Test coverage is layered sensibly across core, provider, component, and solution-wide integration suites.

### Gaps and opportunities

1. **Refresh/cache ownership needed tightening**
   - The refresh flow invalidated the cache and then wrote the same metadata snapshot again even when aggregation had already repopulated it.
   - This created avoidable cache churn on the hot path for manual refresh.
   - The implementation now checks whether aggregation already restored the cache before writing a fallback entry.

2. **Architecture guidance needed a review artifact**
   - The repository had good architectural and ADR documentation, but no single review artifact summarizing strengths, risks, and recommended follow-up work.
   - This file and the next-steps list below close that gap.

## Performance notes

- Metadata refresh is already single-flight per service instance, which is the main concurrency safeguard on the refresh path.
- The cache-write guard added in this change removes an unnecessary write when aggregation has already persisted fresh metadata.
- The biggest remaining performance opportunities are likely to come from provider-level metadata batching and cache invalidation strategy rather than UI changes.

## Test coverage added in this review

- Added coverage proving refresh does not duplicate cache writes when aggregation already repopulates the cache.
- Existing coverage still verifies refresh falls back to storing metadata when aggregation does not do so directly.

## Next steps

1. **Document cache ownership explicitly in service contracts**
   - Clarify whether metadata aggregation is the canonical owner of cache population or whether refresh should remain defensive forever.
   - If aggregation owns caching, make that guarantee explicit in abstractions and test helpers.

2. **Add provider-focused performance tests**
   - Measure metadata refresh behavior for large schema counts and high object counts.
   - Focus on table/view detail fan-out and provider round-trip counts.

3. **Introduce targeted cache metrics**
   - Capture cache hits, misses, refresh duration, and partial-failure counts with sanitized diagnostics.
   - Keep metrics development-only and free of secrets.

4. **Prioritize capability roadmap work**
   - Add feature planning for non-SQL Server providers, richer object definitions, and larger-schema usability improvements.
   - Keep provider-specific work inside provider projects and preserve the existing request/response contract model.
