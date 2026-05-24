# Sample Application

The `samples/` folder contains a self-contained Aspire solution that demonstrates Data Explorer against a realistic EF Core + SQL Server schema.

## Projects

| Project | Description |
|---|---|
| `OakIdeas.Aspire.DataExplorer.Sample.AppHost` | Aspire AppHost — provisions SQL Server, API, and web UI |
| `OakIdeas.Aspire.DataExplorer.Sample.Api` | Minimal API + EF Core model, migrations, and deterministic seed data |
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

## Running the sample

```bash
dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

The Aspire dashboard opens automatically (`http://localhost:15888`). Open `sample-web` and navigate to `/todos`.

## Migrations and seed data

The API applies EF Core migrations automatically on startup:

- Migration assembly: `samples/OakIdeas.Aspire.DataExplorer.Sample.Api/Migrations`
- Startup migration call: `db.Database.MigrateAsync()` in `Sample.Api/Program.cs`

To create a new migration after model changes:

```bash
dotnet ef migrations add <MigrationName> \
  --project samples/OakIdeas.Aspire.DataExplorer.Sample.Api \
  --startup-project samples/OakIdeas.Aspire.DataExplorer.Sample.Api
```

Seed data is configured in `SampleDbContext.OnModelCreating(...)` and is deterministic so screenshots and metadata checks remain stable.

The `ShowcaseProgrammabilityObjects` migration also creates schema-scoped SQL objects in the `showcase` schema and mirrors data from `dbo` tables so Object Explorer can demonstrate folder-based nodes for tables, views, and programmability.

## Validating with Data Explorer

1. Start the sample AppHost only:
  ```bash
  dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
  ```
2. Open `sample-data-explorer` from the Aspire dashboard.
3. Confirm the discovered database is `sampledb`.
4. Inspect tables and metadata for keys, constraints, indexes, and relationships.
5. Use `sample-web` to create/edit/delete/complete/reopen tasks and add comments, then refresh metadata views.
6. On `/todos`, verify the **Schema + Programmability Showcase** card loads counts/rows from:
   - `showcase.vwTodoReplicaOverview`
   - `showcase.usp_ListReplicaTodosByStatus`
   - `showcase.ufn_OpenReplicaTodoCount`

The sample intentionally runs Data Explorer as a consumer-style setup: the AppHost enables discovery with `builder.AddDataExplorer()` and hosts the Data Explorer web resource alongside sample resources.

## E2E validation assets

- SQL setup script: [`docs/samples/test-database-setup.sql`](./test-database-setup.sql)
- Validation guide/checklist: [`docs/samples/e2e-validation-checklist.md`](./e2e-validation-checklist.md)
