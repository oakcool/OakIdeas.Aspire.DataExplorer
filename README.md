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

dotnet run --project /home/runner/work/OakIdeas.Aspire.DataExplorer/OakIdeas.Aspire.DataExplorer/src/OakIdeas.Aspire.DataExplorer.AppHost
```

Open the DataExplorer web endpoint from the Aspire dashboard.

## Solution layout

- `src/` application and libraries
- `tests/` test projects
- `samples/` sample app
- `docs/` architecture, decisions, setup, provider, usage guidance

## Development-only guardrails

- Web runtime startup throws outside `Development`
- Hosting extension startup throws outside `Development`
- UI includes persistent warning banner

## Current milestone status

This commit establishes the initial solution structure and baseline projects for Milestone 1.
