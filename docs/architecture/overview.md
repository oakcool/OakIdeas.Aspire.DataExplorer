# Architecture Overview

OakIdeas.Aspire.DataExplorer is split into UI, orchestration, contracts, and provider layers.

- `Web` hosts the Blazor Server UI.
- `Core` contains abstractions and domain models.
- `Contracts` contains request/response DTOs.
- `Data` contains provider-independent data helpers.
- `SqlServer` provides SQL Server-specific behavior.
- `Hosting` contains Aspire integration extensions.
- `AppHost` orchestrates local development resources.

## Metadata root contracts

Metadata discovery uses provider-agnostic contracts in `Contracts/Models/DatabaseMetadataContracts.cs`:

- `DatabaseMetadataRoot` captures database-level metadata (`DatabaseName`, `ProviderType`, `ResourceId`, collection timestamp, and grouped object maps).
- `DatabaseObject` is the normalized base type (`ObjectId`, `ObjectName`, `FullyQualifiedName`, `ObjectType`, `Description`, `ProviderMetadata`, and `Relationships`).
- Derived object types (`SchemaObject`, `TableObject`, `ViewObject`, `ProcedureObject`, `FunctionObject`, `TriggerObject`) keep a consistent schema-qualified naming model.

`ProviderMetadata` is intentionally a flexible key/value bag (`IReadOnlyDictionary<string, object?>`) so provider projects can add provider-specific values (for example SQL Server object identifiers) without changing shared contracts.

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
