# OakIdeas.Aspire.DataExplorer

OakIdeas.Aspire.DataExplorer is a development-time-only Aspire add-on for inspecting and working with Aspire-hosted databases during local development.

## What it is

- Local development tool
- Aspire AppHost add-on
- SQL Server-first database explorer
- Query and metadata workflow foundation

## What it is not

- Not for production deployment
- Not a public admin console
- Not a DBA replacement

## Quick start

```bash
dotnet restore

dotnet build OakIdeas.Aspire.DataExplorer.sln

dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost
```

The Aspire dashboard opens automatically in your browser.

To run both AppHosts side-by-side during development:

- Start `src/OakIdeas.Aspire.DataExplorer.AppHost` for DataExplorer development
- Start `samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost` for the sample consuming DataExplorer
- In Visual Studio, use the `DataExplorer + Sample` solution launch profile

## Metadata discovery overview

Metadata discovery is provider-driven and aggregated in `Core` services:

1. Discover resources from Aspire hosting context.
2. Select a database resource in scoped UI context.
3. Aggregate metadata through provider discovery interfaces.
4. Cache metadata snapshots by `(resourceId, databaseName)`.
5. Refresh via explicit cache invalidation + re-aggregation.

See:

- [Architecture overview](docs/architecture/overview.md)
- [Metadata discovery architecture](docs/architecture/metadata-discovery.md)
- [Architecture review and next steps](docs/architecture/architecture-review-2026-05.md)
- [Usage quickstart](docs/usage/quickstart.md)

## Metadata feature completeness (current)

Current SQL Server MVP metadata types:

- Schemas
- Tables
- Views
- Columns
- Primary keys
- Foreign keys
- Indexes
- Constraints
- Stored procedures
- Functions
- Triggers
- Object definitions (when available)

## Query Window (SQL Server MVP)

- Execute ad-hoc SQL against the currently selected database context.
- Use `Ctrl+Enter` to execute and `Cancel` to stop long-running queries.
- Results render in a dynamic grid with execution duration, row count, and affected-row metrics when available.
- Result sets are capped by `OakIdeas:Aspire:DataExplorer:MaxQueryRows` and show truncation status.
- Query Window keeps in-memory query history for the active browser session.

### Query options

`DataExplorerOptions` supports:

- `EnableAdHocQueries`
- `EnableWriteOperations` (read-only mode when disabled)
- `QueryTimeoutSeconds`
- `MaxQueryRows`

## E2E validation status

- Integration E2E coverage is in `src/tests/OakIdeas.Aspire.DataExplorer.IntegrationTests/EndToEndValidationIntegrationTests.cs`
- Sample validation database setup script: `docs/samples/test-database-setup.sql`
- Validation workflow checklist: `docs/samples/e2e-validation-checklist.md`

## Troubleshooting

- [Troubleshooting common errors](docs/troubleshooting/error-handling.md)
- [Error handling architecture](docs/architecture/error-handling.md)
- [Local development setup](docs/setup/local-development.md)

## Solution layout

- `src/` application and libraries
- `src/tests/` test projects
- `samples/` sample app
- `docs/` architecture, decisions, setup, provider, usage guidance

### Virtual solution folders

| Folder | Projects |
|---|---|
| `01 - Packages` | Hosting, Contracts, Web.Components |
| `02 - Services` | Web |
| `03 - Data` | Data, SqlServer |
| `04 - Core` | Core |
| `01 - Packages/Tests` | Web.Components.Tests |
| `02 - Services/Tests` | Web.Tests |
| `03 - Data/Tests` | Data.Tests, SqlServer.Tests |
| `04 - Core/Tests` | Core.Tests |
| `06 - Orchestration` | AppHost |
| `07 - Tests` | Solution-wide tests (for example: IntegrationTests) |
| `08 - Samples` | Sample.AppHost, Sample.Api, Sample.Web, Sample.Web.Components |
| `08 - Samples/Tests` | Sample.Web.Components.Tests |

Sample projects remain organized under `08 - Samples` and are kept separate from the numbered non-sample sections.

## Development-only guardrails

- Web runtime startup throws outside `Development`
- Hosting extension startup throws outside `Development`
- UI includes persistent warning banner
