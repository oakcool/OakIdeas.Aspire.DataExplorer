# OakIdeas.Aspire.DataExplorer

OakIdeas.Aspire.DataExplorer is a development-time-only Aspire add-on for exploring Aspire-hosted databases during local development. It gives Aspire applications a focused database explorer, metadata browser, and guarded SQL query surface without weakening the project's development-only boundary.

## Why it exists

Aspire makes it easy to stand up local application environments, but inspecting the databases inside those environments still usually means switching tools and reconstructing connection details manually. Data Explorer keeps that workflow inside the Aspire development experience so contributors can inspect schema metadata, validate local data, and run safe troubleshooting queries faster.

## Key features

- Aspire-hosted database discovery for local development
- SQL Server-first metadata exploration for schemas, tables, views, procedures, functions, triggers, and definitions
- Query Window with read-only mode support, row limits, timeout controls, and execution-plan integration
- Development-only runtime and hosting guardrails
- Provider-based architecture that keeps provider-specific SQL and error mapping isolated
- Sample Aspire application for consumer-style validation

## Quick start

### Prerequisites

- .NET SDK 10.0+
- Node.js 20+
- Docker Desktop or another container runtime supported by Aspire

### Run the solution

```bash
dotnet restore
dotnet build OakIdeas.Aspire.DataExplorer.sln
dotnet test OakIdeas.Aspire.DataExplorer.sln
dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost
```

The Aspire dashboard opens automatically and exposes the Data Explorer web resource for the local environment.

## Use the application

1. Open the Data Explorer app from the Aspire dashboard.
2. Select a discovered database resource.
3. Browse metadata in Object Explorer.
4. Open **Query** to run ad-hoc SQL with the configured guardrails.
5. Optionally start `samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost` to validate a consumer-style setup.

## Website

- [Project website](https://oakcool.github.io/OakIdeas.Aspire.DataExplorer/)
- [Website maintenance guide](docs/publishing/website-guide.md)

## Documentation

- [Documentation guide](docs/README.md)
- [Website guide](docs/publishing/website-guide.md)
- [Getting started and local development](docs/setup/local-development.md)
- [Architecture overview](docs/architecture/overview.md)
- [Metadata discovery architecture](docs/architecture/metadata-discovery.md)
- [Sample application and validation walkthrough](docs/samples/README.md)
- [NuGet package README](docs/nuget/package-readme.md)
- [Package publishing guide](docs/publishing/package-publishing-guide.md)
- [Troubleshooting](docs/troubleshooting/error-handling.md)

## Repository layout

- `src/` application and library projects
- `src/tests/` project-specific and solution-wide automated tests
- `samples/` sample Aspire consumer application
- `docs/` public documentation, architecture notes, publishing guidance, and samples

## Contributing and release notes

- [Contributing guide](CONTRIBUTING.md)
- [Changelog](CHANGELOG.md)
- [License](LICENSE)

## Development-only guardrails

- Web runtime startup throws outside `Development`
- Hosting extension startup throws outside `Development`
- Query behavior stays behind explicit options and safe diagnostics
- Connection strings and secrets are kept out of client-side code
