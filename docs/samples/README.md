# Sample Application

The `samples/` folder contains a self-contained Aspire application that demonstrates how to integrate OakIdeas.Aspire.DataExplorer into your own project.

## Projects

| Project | Description |
|---|---|
| `OakIdeas.Aspire.DataExplorer.SampleApp` | Aspire AppHost — orchestrates all services |
| `OakIdeas.Aspire.DataExplorer.Sample.Api` | Minimal API with EF Core SQL Server and migrations |
| `OakIdeas.Aspire.DataExplorer.Sample.Web` | Blazor Server app consuming the API |

## What it demonstrates

- How to call `AddDataExplorer()` in the AppHost
- A running SQL Server with EF Core migrations applied on startup
- A Blazor frontend managing `TodoItems` via a REST API
- The DataExplorer web tool connected to the same SQL Server instance

## Running the sample

```bash
dotnet run --project samples/OakIdeas.Aspire.DataExplorer.SampleApp
```

The Aspire dashboard opens automatically at `http://localhost:15888`.

Navigate to:
- **sample-web** — the Blazor frontend
- **data-explorer** — the DataExplorer tool pointing at the SQL Server
