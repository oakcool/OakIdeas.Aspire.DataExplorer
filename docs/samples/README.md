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
- How the sample AppHost runs side-by-side with the main DataExplorer AppHost

## Running the sample

```bash
dotnet run --project samples/OakIdeas.Aspire.DataExplorer.SampleApp
```

The Aspire dashboard opens automatically at `http://localhost:15888`.

Navigate to:
- **sample-web** — the Blazor frontend

To run the DataExplorer tool at the same time, start the main AppHost in a second terminal:

```bash
dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost
```
