# Architecture Overview

OakIdeas.Aspire.DataExplorer is split into UI, orchestration, contracts, and provider layers.

- `Web` hosts the Blazor Server UI.
- `Core` contains abstractions and domain models.
- `Contracts` contains request/response DTOs.
- `Data` contains provider-independent data helpers.
- `SqlServer` provides SQL Server-specific behavior.
- `Hosting` contains Aspire integration extensions.
- `AppHost` orchestrates local development resources.

## Solution virtual folder layout

| Folder | Projects |
|---|---|
| `01 - Packages` | Hosting, Web, Contracts |
| `02 - Services` | _(reserved)_ |
| `03 - Data` | Data, SqlServer |
| `04 - Core` | Core |
| `05 - Tools` | All test projects |
| `06 - Orchestration` | AppHost |
| `07 - Samples` | Sample.AppHost, Sample.Api, Sample.Web |
