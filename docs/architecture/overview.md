# Architecture Overview

OakIdeas.Aspire.DataExplorer is split into UI, orchestration, contracts, and provider layers.

- `Web` hosts the Blazor Server UI.
- `Core` contains abstractions and domain models.
- `Contracts` contains request/response DTOs.
- `Data` contains provider-independent data helpers.
- `SqlServer` provides SQL Server-specific behavior.
- `Hosting` contains Aspire integration extensions.
- `AppHost` orchestrates local development resources.
