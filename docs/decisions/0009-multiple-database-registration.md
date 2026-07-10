# ADR 0009: Multiple database registration for a single Data Explorer instance

## Status
Accepted

## Context
Data Explorer originally focused on a single selected database within one running instance, but Aspire applications often expose multiple database resources during local development. The configuration model needs to support multiple resources without weakening development-only guardrails, introducing provider-specific behavior into shared layers, or breaking the existing single-database setup.

## Decision
- Keep `AddDataExplorer()` as the single entry point for creating one Data Explorer resource.
- Continue provider enablement through provider-specific extensions such as `.AddSqlServer()`.
- Register each database resource by repeating Aspire's existing `.WithReference(...)` pattern on the Data Explorer resource.
- Discover all referenced connection strings at runtime and project them into `DiscoveredDatabaseResource` entries.
- Keep user state scoped to one selected database at a time, while caching and metadata isolation continue to use the existing `(resourceId, databaseName)` key model.
- Preserve backward compatibility: a single `.WithReference(database)` call remains valid and behaves the same as before.

## Alternatives considered
1. **Multiple resource-specific parameters on `AddDataExplorer(...)`**
   - Rejected because it is less idiomatic than Aspire's repeated resource-builder composition and becomes awkward as resource counts grow.
2. **A custom collection-based registration API**
   - Rejected because it hides the familiar Aspire resource graph, reduces discoverability of resource dependencies, and complicates incremental configuration.
3. **A bespoke fluent database-registration builder**
   - Rejected because it duplicates Aspire concepts, adds another abstraction surface to maintain, and provides little value over repeated `.WithReference(...)`.
4. **Implicit discovery of every database in the AppHost**
   - Rejected because Data Explorer should only see resources explicitly wired into its development resource graph.

## Consequences
- Single-database consumers keep their existing setup unchanged.
- Multi-database consumers can add more resources incrementally with repeated `.WithReference(...)` calls.
- Database switching stays explicit in the UI and service layer.
- Provider-specific logic remains isolated in provider packages; shared layers only manage contracts, selection, discovery orchestration, and state isolation.
