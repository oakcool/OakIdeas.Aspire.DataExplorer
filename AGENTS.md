# AGENTS.md

## Project Purpose

OakIdeas.Aspire.DataExplorer is a development-time-only Aspire add-on for exploring Aspire-hosted databases.

## Hard Rules

- Do not add production deployment support.
- Do not weaken development-only runtime guards.
- Do not put connection strings or secrets into client-side code.
- Keep provider-specific logic in provider projects.

## Architecture Rules

- Use request/response models for service operations.
- Keep UI components focused and composable.
- Use dependency injection and async APIs with `CancellationToken`.
- Keep generated SQL parameterized.
- Keep Aspire resource discovery provider-agnostic in shared contracts and core logic; place Aspire-specific enumeration in Hosting.
- Register providers through `MetadataProviderFactoryOptions`; keep concrete provider registrations in composition roots or provider projects, never in shared contracts/core models.

## Testing Rules

- Add focused tests for new behavior.
- Add integration tests for provider behavior where practical.
- Place project-specific test projects under the matching solution section's `Tests` virtual folder.
- Keep `07 - Tests` reserved for solution-wide test coverage (for example, integration and usability suites).
- Validate build and tests before completing work.

## Documentation Rules

- Update docs when architecture or behavior changes.
- Add ADRs for significant decisions.
