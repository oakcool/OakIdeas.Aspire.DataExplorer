# Local Development Setup

## Prerequisites

- .NET SDK 10.0+
- Node.js 20+ (TailwindCSS build in web projects)
- Docker Desktop or equivalent container runtime
- Aspire workload + templates for local orchestration scenarios

## Development-time-only constraints

- DataExplorer is development-time-only and guarded at runtime/hosting startup.
- Do not deploy DataExplorer as a production administration surface.
- Keep connection strings and secrets out of client-side code and docs.

## Local commands

```bash
dotnet restore

dotnet build OakIdeas.Aspire.DataExplorer.sln

dotnet test OakIdeas.Aspire.DataExplorer.sln

dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost

dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

## Database setup for testing

- Start AppHost and let Aspire provision SQL Server resources.
- Use sample AppHost when validating consumer integration.
- Current integration tests focus on metadata projection/normalization with provider row shapes (not a live SQL Server dependency).

## TailwindCSS

TailwindCSS v4 is integrated into `OakIdeas.Aspire.DataExplorer.Web` and `OakIdeas.Aspire.DataExplorer.Sample.Web`.

- Build runs automatically during `dotnet build`.
- Output file (`tailwind.output.css`) is generated and ignored by git.
- Optional watch mode (run inside each web project):

```bash
npm run watch:css
```

If using Visual Studio, use `DataExplorer + Sample (Development Debug)` solution launch profile.

## Query Window configuration

`DataExplorerOptions` (section `OakIdeas:Aspire:DataExplorer`) controls query behavior:

- `EnableAdHocQueries` (default `true`)
- `EnableWriteOperations` (set `false` for read-only query mode)
- `QueryTimeoutSeconds` (default `30`)
- `MaxQueryRows` (default `1000`)

When users enable **Include Execution Plan** in Query Window, the query response can include provider execution plan payloads (`MermaidDiagram`, optional `RawPlan`, and availability/message metadata).
