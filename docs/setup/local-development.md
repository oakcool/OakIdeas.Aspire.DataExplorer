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
```
