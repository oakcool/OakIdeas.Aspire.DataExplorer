# Copilot Instructions

- Preserve development-only boundaries.
- Keep changes minimal and focused.
- Prefer SQL Server-first behavior for MVP.
- Keep provider abstractions intact; do not place provider-specific logic in shared layers.
- Keep provider SQL, discovery behavior, and provider-specific error mapping in provider projects.
- Use request/response contracts for service and discovery operations.
- Use shared error contracts and sanitized diagnostics for user-visible failures.
- Add or update tests when behavior changes.
- For new metadata discoverers/providers, add focused tests and prefer TDD-style contract-first implementation.
- Place project-specific test projects under the matching solution section `Tests` folder.
- Keep test project directories under `src/tests`.
- Reserve `07 - Tests` for solution-wide test suites.
- Keep sample projects and sample tests grouped under `08 - Samples` and separate from non-sample solution folder rules.
- Update docs for architecture-impacting changes.

## Example references

- Provider implementation walkthrough: `docs/providers/implementation-guide.md`
- Provider model and registration: `docs/architecture/provider-model.md`
- Metadata discovery architecture: `docs/architecture/metadata-discovery.md`
