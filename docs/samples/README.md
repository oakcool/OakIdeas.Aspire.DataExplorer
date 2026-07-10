# Sample Application

The `samples/` folder contains a self-contained Aspire solution that demonstrates Data Explorer against two realistic EF Core + SQL Server schemas.

## Projects

| Project | Description |
|---|---|
| `OakIdeas.Aspire.DataExplorer.Sample.AppHost` | Aspire AppHost — provisions SQL Server, two database resources, API, and web UI |
| `OakIdeas.Aspire.DataExplorer.Sample.Api` | Minimal API + EF Core models, migrations, and deterministic seed data for both sample databases |
| `OakIdeas.Aspire.DataExplorer.Sample.Web` | Blazor Server app with forms, filters, detail views, and comment workflows |

## Sample schema goals

The sample database intentionally exercises metadata discovery scenarios in Data Explorer:

- Primary/foreign keys
- Many-to-many (`TodoItemTag`)
- Lookup/reference tables (`TodoStatus`, `TodoPriority`)
- Nullable + non-nullable columns
- Default values and check constraints
- Unique indexes (`Name` columns on lookup tables)
- Date/time, boolean, and text fields
- Seeded relational data with comments and tags
- Additional schema-scoped objects for explorer demos:
  - `showcase` schema
  - mirror tables (`showcase.TodoListsReplica`, `showcase.TodoItemsReplica`)
  - view (`showcase.vwTodoReplicaOverview`)
  - stored procedure (`showcase.usp_ListReplicaTodosByStatus`)
  - scalar function (`showcase.ufn_OpenReplicaTodoCount`)

Main entities:

- `TodoItem`
- `TodoList`
- `TodoCategory`
- `TodoTag`
- `TodoItemTag`
- `TodoPriority`
- `TodoStatus`
- `TodoComment`

Secondary warehouse entities:

- `WarehouseSupplier`
- `WarehouseLocation`
- `WarehouseInventoryItem`
- `WarehouseStockBin`

## Running the sample

```bash
dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

The Aspire dashboard opens automatically (`http://localhost:15888`). Open `sample-web` and navigate to `/todos`.

## Migrations and seed data

The API applies EF Core migrations automatically on startup for both `sampledb` and `warehousedb`:

- Migration assembly: `samples/OakIdeas.Aspire.DataExplorer.Sample.Api/Migrations`
- Warehouse migration assembly: `samples/OakIdeas.Aspire.DataExplorer.Sample.Api/MigrationsWarehouse`
- Startup migration call: `db.Database.MigrateAsync()` in `Sample.Api/Program.cs`

To create a new migration after model changes:

```bash
dotnet ef migrations add <MigrationName> \
  --context SampleDbContext \
  --project samples/OakIdeas.Aspire.DataExplorer.Sample.Api \
  --startup-project samples/OakIdeas.Aspire.DataExplorer.Sample.Api

dotnet ef migrations add <MigrationName> \
  --context WarehouseDbContext \
  --output-dir MigrationsWarehouse \
  --project samples/OakIdeas.Aspire.DataExplorer.Sample.Api \
  --startup-project samples/OakIdeas.Aspire.DataExplorer.Sample.Api
```

Seed data is configured in `SampleDbContext.OnModelCreating(...)` and is deterministic so validation output remains stable.

The `ShowcaseProgrammabilityObjects` migration also creates schema-scoped SQL objects in the `showcase` schema and mirrors data from `dbo` tables so Object Explorer can demonstrate folder-based nodes for tables, views, and programmability.

## Validating with Data Explorer

### Automated validation

- Integration coverage lives in `src/tests/OakIdeas.Aspire.DataExplorer.IntegrationTests/EndToEndValidationIntegrationTests.cs`.
- Run the solution-wide test suite with `dotnet test OakIdeas.Aspire.DataExplorer.sln`.
- If you need a focused sample validation run, use:

```bash
dotnet test src/tests/OakIdeas.Aspire.DataExplorer.IntegrationTests/OakIdeas.Aspire.DataExplorer.IntegrationTests.csproj
```

### Manual walkthrough

1. Start `samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost`.
2. Open `data-explorer` from the Aspire dashboard.
3. Confirm the discovered databases include both `sampledb` and `warehousedb`.
4. Inspect tables and metadata for keys, constraints, indexes, and relationships in `sampledb`.
   - Metadata leaves in Object Explorer and details use compact parenthetical formatting.
   - Common columns use the `ViewColumns` icon; PK/FK/parameter metadata uses `Key`/`Link`/`AtSymbol` icons.
5. Use `sample-web` to create/edit/delete/complete/reopen tasks and add comments, then refresh metadata views.
   - Confirm scrollbars remain consistent across Object Explorer, Explorer details, Query, and Execution Plan surfaces.
6. Switch the picker to `warehousedb` and verify the warehouse schema exposes suppliers, locations, inventory items, and stock bins with independent keys, indexes, and relationships.
7. On `/todos`, verify the **Schema + Programmability Showcase** card loads counts/rows from:
   - `showcase.vwTodoReplicaOverview`
   - `showcase.usp_ListReplicaTodosByStatus`
   - `showcase.ufn_OpenReplicaTodoCount`

### Local SQL setup asset

- SQL setup script: [`docs/samples/test-database-setup.sql`](./test-database-setup.sql)

The sample intentionally runs Data Explorer as a consumer-style setup: the AppHost enables discovery with `builder.AddDataExplorer().AddSqlServer()` and references both `sampledb` and `warehousedb` so the database picker can demonstrate independent resource switching.
