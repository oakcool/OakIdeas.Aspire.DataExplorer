# OakIdeas.Aspire.DataExplorer

OakIdeas.Aspire.DataExplorer is a development-time-only Aspire add-on for inspecting and working with Aspire-hosted databases during local development.

## What it is

- Local development tool
- Aspire AppHost add-on
- SQL Server-first database explorer
- Query and table workflow foundation

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

## Solution layout

- `src/` application and libraries
- `tests/` test projects
- `samples/` sample app
- `docs/` architecture, decisions, setup, provider, usage guidance

### Virtual solution folders

| Folder | Projects |
|---|---|
| `01 - Packages` | Hosting, Web, Contracts |
| `02 - Services` | _(reserved)_ |
| `03 - Data` | Data, SqlServer |
| `04 - Core` | Core |
| `05 - Tools` | All test projects |
| `06 - Orchestration` | AppHost |
| `07 - Samples` | Sample.AppHost, Sample.Api, Sample.Web |

## Development-only guardrails

- Web runtime startup throws outside `Development`
- Hosting extension startup throws outside `Development`
- UI includes persistent warning banner

## Current milestone status

This commit establishes the initial solution structure and baseline projects for Milestone 1.
