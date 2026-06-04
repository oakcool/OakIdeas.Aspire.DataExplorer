# OakIdeas.Aspire.DataExplorer packages

## Project overview

OakIdeas.Aspire.DataExplorer provides development-time tooling for exploring Aspire-hosted databases.

## Features

- Aspire hosting integration package (`OakIdeas.Aspire.DataExplorer.Hosting`)
- Shared contracts package (`OakIdeas.Aspire.DataExplorer.Contracts`)
- Reusable Blazor components package (`OakIdeas.Aspire.DataExplorer.Web.Components`)
- Blazor Server web package (`OakIdeas.Aspire.DataExplorer.Web`)
- SQL Server-first metadata and query tooling support

## Installation

```bash
dotnet add package OakIdeas.Aspire.DataExplorer.Hosting --version <version>
```

You can also install `OakIdeas.Aspire.DataExplorer.Contracts`, `OakIdeas.Aspire.DataExplorer.Web.Components`, or `OakIdeas.Aspire.DataExplorer.Web` when only those package layers are required.

## Basic usage

```csharp
builder.AddDataExplorer();
```

For full usage guidance, see repository documentation.

## Supported frameworks

- .NET 10 (`net10.0`)

## Versioning strategy

- Stable releases: `x.y.z`
- Preview releases: `x.y.z-preview.n`

Stable releases are promoted after validation and approval. Preview releases are independently publishable for pre-release adoption.

## Contributing

See the contributor package guide:

- `docs/publishing/contributor-package-guide.md`

## Links

- Source: https://github.com/oakcool/OakIdeas.Aspire.DataExplorer
- Documentation: https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/tree/main/docs
