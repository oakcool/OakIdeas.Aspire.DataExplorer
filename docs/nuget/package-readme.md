# OakIdeas.Aspire.DataExplorer packages

## Project overview

OakIdeas.Aspire.DataExplorer provides development-time tooling for exploring Aspire-hosted databases.

## Features

- Core Aspire integration package (`OakIdeas.Aspire.DataExplorer`)
- SQL Server provider package (`OakIdeas.Aspire.DataExplorer.SqlServer`)
- SQL Server-first metadata and query tooling support

## Installation

```bash
dotnet add package OakIdeas.Aspire.DataExplorer --version <version>
dotnet add package OakIdeas.Aspire.DataExplorer.SqlServer --version <version>
```

## Basic usage

```csharp
builder.AddDataExplorer()
	.AddSqlServer();
```

For full usage guidance, see the repository README and documentation guide.

## Supported frameworks

- .NET 10 (`net10.0`)

## Versioning strategy

- Stable releases: `x.y.z`
- Preview releases: `x.y.z-preview.n`

Stable releases are promoted after validation and approval. Preview releases are independently publishable for pre-release adoption.

## Contributing

See the repository contributing and packaging guides:

- `CONTRIBUTING.md`
- `docs/publishing/contributor-package-guide.md`

## Links

- Source: https://github.com/oakcool/OakIdeas.Aspire.DataExplorer
- Website: https://oakcool.github.io/OakIdeas.Aspire.DataExplorer/
- Documentation: https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/tree/main/docs
- Contributing: https://github.com/oakcool/OakIdeas.Aspire.DataExplorer/blob/main/CONTRIBUTING.md
