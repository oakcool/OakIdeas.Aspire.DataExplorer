# AGENTS.md

## Project Purpose

OakIdeas.Aspire.DataExplorer is a development-time-only Aspire add-on for exploring Aspire-hosted databases.

## Hard Rules

- Do not add production deployment support.
- Do not weaken development-only runtime guards.
- Do not put connection strings or secrets into client-side code.
- Keep provider-specific logic in provider projects.
- Keep Query Window guardrails enabled (ad-hoc toggle, read-only mode support, safe diagnostics).

## Architecture Rules

- Use request/response models for service operations.
- Keep UI components focused and composable.
- Use dependency injection and async APIs with `CancellationToken`.
- Keep generated SQL parameterized.
- Route user-visible failures through shared error contracts and sanitized diagnostics.
- Keep Aspire resource discovery provider-agnostic in shared contracts and core logic; place Aspire-specific enumeration in Hosting.
- Register providers through `MetadataProviderFactoryOptions`; keep concrete provider registrations in composition roots or provider projects, never in shared contracts/core models.
- Keep provider isolation strict: shared layers define contracts/orchestration, provider projects own SQL, provider exception mapping, and capability-specific discovery behavior.

## Testing Rules

- Add focused tests for new behavior.
- Add integration tests for provider behavior where practical.
- Use TDD for new metadata discoverers where feasible: define request/response contract behavior first, then implement provider discovery.
- For new providers/discovery services, test contract mapping, normalization/projection, and error mapping paths.
- Place project-specific test projects under the matching solution section's `Tests` virtual folder.
- Keep test project directories under `src/tests`.
- Keep `07 - Tests` reserved for solution-wide test coverage (for example, integration and usability suites).
- Keep sample projects and sample tests grouped under `08 - Samples` and separate from non-sample solution folder rules.
- Validate build and tests before completing work.

## Error Handling Expectations

- Use `IErrorHandler` for user-visible failures.
- Keep provider-specific exception mapping in `IProviderErrorMapper` implementations inside provider projects.
- Do not expose secrets (connection strings, credentials, machine paths) in messages, diagnostics, or logs.

## Implementation Examples

- New provider/discovery implementation guide: `docs/providers/implementation-guide.md`
- Provider model and registration rules: `docs/architecture/provider-model.md`
- Metadata discovery architecture and request/response patterns: `docs/architecture/metadata-discovery.md`

## Documentation Rules

- Update docs when architecture or behavior changes.
- Add ADRs for significant decisions.
- Document new error categories, recovery guidance, and safe logging expectations when diagnostics behavior changes.
- For any UI change, include before/after screenshots in the pull request description.
