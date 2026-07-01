# Contributing

Thanks for your interest in OakIdeas.Aspire.DataExplorer. This repository is focused on a development-time-only Aspire database exploration experience, so please keep that boundary intact when proposing changes.

## Before you start

- Read the [documentation guide](docs/README.md) for the current docs map.
- Review the [architecture overview](docs/architecture/overview.md) and [provider model](docs/architecture/provider-model.md) before making architecture-affecting changes.
- Keep provider-specific SQL, discovery behavior, and error mapping inside provider projects.

## Local setup

```bash
dotnet restore
dotnet build OakIdeas.Aspire.DataExplorer.sln
dotnet test OakIdeas.Aspire.DataExplorer.sln
```

For AppHost, sample, Tailwind, and query configuration details, see [docs/setup/local-development.md](docs/setup/local-development.md).

## Development expectations

- Preserve development-only runtime and hosting guards.
- Use request/response contracts for service and discovery operations.
- Keep user-visible failures sanitized through the shared error contracts.
- Keep generated SQL parameterized.
- Add focused tests when behavior changes.
- Update relevant documentation when public behavior, architecture, or packaging changes.

## Pull requests

When opening a pull request, include:

- A concise summary of the change
- Validation details (`dotnet build`, `dotnet test`, and any targeted manual checks)
- UI before/after screenshots when the change affects layout, styling, or user-facing visuals

## Packaging and releases

Public package guidance lives in:

- [docs/nuget/package-readme.md](docs/nuget/package-readme.md)
- [docs/publishing/contributor-package-guide.md](docs/publishing/contributor-package-guide.md)
- [docs/publishing/package-publishing-guide.md](docs/publishing/package-publishing-guide.md)
