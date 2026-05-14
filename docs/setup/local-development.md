# Local Development Setup

## Prerequisites

- .NET SDK 10.0+
- Docker (for SQL Server container via Aspire)

## Commands

```bash
dotnet restore

dotnet build OakIdeas.Aspire.DataExplorer.sln

dotnet test OakIdeas.Aspire.DataExplorer.sln

dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost

dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

Both AppHosts are configured to open the Aspire dashboard in your browser automatically.

If you are using Visual Studio, select the `DataExplorer + Sample` solution launch profile to start both AppHosts together.
Use `DataExplorer + Sample (Debug Development)` for a debugging-focused launch that uses each AppHost `https` profile and Development environment settings.
