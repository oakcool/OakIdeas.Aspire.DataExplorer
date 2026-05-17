# Architecture Overview

OakIdeas.Aspire.DataExplorer is split into UI, orchestration, contracts, and provider layers.

- `Web` hosts the Blazor Server UI.
- `Core` contains abstractions and domain models.
- `Contracts` contains request/response DTOs.
- `Data` contains provider-independent data helpers.
- `SqlServer` provides SQL Server-specific behavior.
- `Hosting` contains Aspire integration extensions.
- `AppHost` orchestrates local development resources.

## Selected database context service

Database targeting is handled by `ISelectedDatabaseService` in `Core`:

- Stores the in-memory selected `DiscoveredDatabaseResource` as `SelectedDatabaseContext` per scoped session.
- Validates selections against `IAspireResourceDiscovery` before switching context.
- Exposes async APIs (`SelectDatabaseAsync`, `GetSelectedDatabaseAsync`, `ClearSelectionAsync`, `IsSelectedAsync`) and a `SelectionChanged` notification event for reactive UI flows.

## Metadata root contracts

Metadata discovery uses provider-agnostic contracts in `Contracts/Models/DatabaseMetadataContracts.cs`:

- `DatabaseMetadataRoot` captures database-level metadata (`DatabaseName`, `ProviderType`, `ResourceId`, collection timestamp, and grouped object maps).
- `DatabaseObject` is the normalized base type (`ObjectId`, `ObjectName`, `FullyQualifiedName`, `ObjectType`, `Description`, `ProviderMetadata`, and `Relationships`).
- Derived object types (`SchemaObject`, `TableObject`, `ViewObject`, `ProcedureObject`, `FunctionObject`, `TriggerObject`) keep a consistent schema-qualified naming model.

`ProviderMetadata` is intentionally a flexible key/value bag (`IReadOnlyDictionary<string, object?>`) so provider projects can add provider-specific values (for example SQL Server object identifiers) without changing shared contracts.

## Metadata aggregation service

`IMetadataAggregationService` in `Core` composes provider discovery operations into a single snapshot for the selected database:

- Schemas are discovered first.
- Tables/views, programmable objects, and table-level details are collected with async parallel fan-out.
- Aggregation tracks `MetadataCollectionStatus` (`Success`, `PartialSuccess`, `Failed`) and per-operation failure details.
- `InMemoryMetadataCache` stores `DatabaseMetadataRoot` by `(resourceId, databaseName)` with configurable TTL via `MetadataAggregationOptions.CacheTtlMinutes`.
- Cache invalidation is exposed through `IMetadataCache.InvalidateAsync` for refresh and future invalidation workflows.

## Error handling and diagnostics

Error categorization and sanitized diagnostics are documented in `docs/architecture/error-handling.md`.

- `IErrorHandler` creates safe `DataExplorerError` payloads for UI-facing responses.
- Provider-specific exception mapping remains in provider projects through `IProviderErrorMapper`.
- UI components surface recovery suggestions and optional diagnostic metadata without exposing secrets.

## Solution virtual folder layout

| Folder | Projects |
|---|---|
| `01 - Packages` | Hosting, Web, Contracts |
| `02 - Services` | _(reserved)_ |
| `03 - Data` | Data, SqlServer |
| `04 - Core` | Core |
| `01 - Packages/Tests` | Web.Tests |
| `03 - Data/Tests` | Data.Tests, SqlServer.Tests |
| `04 - Core/Tests` | Core.Tests |
| `06 - Orchestration` | AppHost |
| `07 - Tests` | Solution-wide tests (for example: IntegrationTests) |
| `08 - Samples` | Sample.AppHost, Sample.Api, Sample.Web |
