# Local Development Setup

## Prerequisites

- .NET SDK 10.0+
- Node.js 20+ (for TailwindCSS build)
- Docker (for SQL Server container via Aspire)

## Commands

```bash
dotnet restore

dotnet build OakIdeas.Aspire.DataExplorer.sln

dotnet test OakIdeas.Aspire.DataExplorer.sln

dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost

dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

## TailwindCSS

TailwindCSS v4 is integrated into both web projects (`OakIdeas.Aspire.DataExplorer.Web` and `OakIdeas.Aspire.DataExplorer.Sample.Web`). The CSS build runs automatically as part of `dotnet build` via an MSBuild target that calls `npm install` and `npm run build:css`.

To watch for CSS changes during development, run in each web project directory:

```bash
npm run watch:css
```

The generated `tailwind.output.css` file is excluded from version control (`.gitignore`).

Both AppHosts are configured to open the Aspire dashboard in your browser automatically.

If you are using Visual Studio, select the `DataExplorer + Sample (Development Debug)` solution launch profile to start both AppHosts together with each AppHost `https` launch profile (Development environment settings).
