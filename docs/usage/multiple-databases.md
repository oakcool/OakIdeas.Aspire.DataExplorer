# Multiple Database Configuration

## Overview

One Data Explorer instance can explore multiple Aspire database resources at the same time. Each configured resource is discovered independently, shown in the database picker, and resolved through the existing selected-database flow so metadata, query execution, diagrams, and execution plans stay isolated per resource.

## Requirements

- `OakIdeas.Aspire.DataExplorer` package
- A provider package for each database type you want to expose (for SQL Server MVP: `OakIdeas.Aspire.DataExplorer.SqlServer`)
- Explicit `WithReference(...)` wiring from the Data Explorer resource to each database resource

## Single-database configuration

```csharp
var sql = builder.AddSqlServer("sql", password)
    .AddDatabase("applicationdb");

builder.AddDataExplorer()
    .AddSqlServer()
    .WithReference(sql);
```

## Multiple-database configuration

```csharp
var sqlServer = builder.AddSqlServer("sample-sql", password);
var appDatabase = sqlServer.AddDatabase("sampledb");
var warehouseDatabase = sqlServer.AddDatabase("warehousedb");

builder.AddDataExplorer()
    .AddSqlServer()
    .WithReference(appDatabase)
    .WithReference(warehouseDatabase);
```

## How identification works

- Each discovered entry is identified by its Aspire resource ID.
- The UI shows the database name in the picker.
- Metadata caching and refresh isolation use the resource ID plus database name.
- If two registrations would resolve to the same resource ID, Aspire configuration should be corrected before startup.

## Selection and switching

1. Open Data Explorer from the Aspire dashboard.
2. Use the **Database** picker above Object Explorer.
3. Choose the database you want to inspect.
4. Data Explorer reloads Object Explorer and subsequent pages for the selected resource.

When the selected database changes:

- Object Explorer refreshes against the new resource.
- Explorer details use metadata from the new selection.
- Query execution and execution plans use the new selection.
- Diagram pages render relationships from the new selection.

## Backward compatibility

Existing single-database setups continue to work unchanged. Repeated `WithReference(...)` calls are additive, so the single-resource case remains the simplest valid configuration.

## Sample application

The sample AppHost registers two SQL Server database resources:

- `sampledb` — todo/work-tracking domain
- `warehousedb` — warehouse/inventory domain

Run the sample with:

```bash
dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

Then switch between `sampledb` and `warehousedb` in the database picker to verify independent object trees and query results.

## Troubleshooting

- If a database is missing, confirm the Data Explorer resource has a `WithReference(...)` to that database.
- If selection fails, verify the referenced resource is healthy in the Aspire dashboard.
- If metadata appears stale after switching, use **Refresh** in Object Explorer.
- If a query targets the wrong database, confirm the picker shows the intended selection before executing.

## Known limitations

- SQL Server is still the primary supported provider.
- Data Explorer shows one active database selection at a time per browser session.
- Only explicitly referenced database resources are discoverable.
