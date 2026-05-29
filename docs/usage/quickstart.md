# Usage Quickstart

## Metadata discovery walkthrough

1. Run the AppHost:

   ```bash
   dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost
   ```

2. Open the DataExplorer web app from the Aspire dashboard.
3. Resolve `IExplorerService` and list databases:

   ```csharp
   var available = await explorerService.GetAvailableDatabasesAsync(cancellationToken);
   var first = available.Resources.FirstOrDefault();
   if (first is null)
   {
       return;
   }

   var selected = await explorerService.SelectDatabaseAsync(first.ResourceId, cancellationToken);
   if (!selected.Succeeded)
   {
       return;
   }

    var metadata = await explorerService.GetDatabaseMetadataAsync(cancellationToken);
    var refresh = await explorerService.RefreshDatabaseMetadataAsync(cancellationToken);
    var query = await explorerService.ExecuteQueryAsync(
        "SELECT TOP 10 name FROM sys.tables ORDER BY name;",
        includeExecutionPlan: false,
        cancellationToken);
    ```

4. In Object Explorer, browse metadata by:
   - `Schema`
   - `Tables`
   - `Views`
   - `Procedures`
   - `Functions`
   - `Triggers`
   - Tables expand into `Columns`, `Keys`, `Constraints`, `Triggers`, and `Indexes`
   - Procedures expand into `Parameters`
   - Functions expand into `Parameters` and `Return Type`
   - Metadata leaves use compact parenthetical formatting, for example:
     - `Id (PK, identity, int(4), not null)`
     - `PK_TodoItems (Clustered)`
     - `@StatusId (tinyint, Input, Has default)`
   - Metadata icons are semantic:
     - Common columns use `ViewColumns`
     - PK/identity columns use `Key`
     - FK columns use `Link`
     - Parameters use `AtSymbol`
5. Select an object to load object details and (when supported) definition text.
   - Table details show columns, keys, constraints, triggers, and indexes
   - Procedure details show parameter type, direction, and default availability
   - Function details show parameters, return type, and function kind
   - Details panels use the same compact parenthetical metadata style as Object Explorer
   - Scrollbars use the shared app theme across explorer/details/query/plan surfaces
6. Open **Query** in the top navigation to run ad-hoc SQL against the selected database.
   - `Ctrl+Enter` executes the current query.
   - `Cancel` requests query cancellation.
   - `Include Execution Plan` requests provider execution plan data (when supported).
   - Execution plan output is shown in the **Execution Plan** result tab.
   - Destructive statements require an explicit confirmation run.

## Running the sample with metadata exploration

Run sample AppHost to validate a consuming app workflow:

```bash
dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost
```

Use the Aspire dashboard to open the sample web app and verify metadata tree navigation and refresh behavior.

## Troubleshooting

- If databases are missing, wait for resources to become healthy, then refresh.
- If refresh is already running, wait for completion and retry.
- If metadata load fails, use the diagnostics code and follow recovery guidance in the UI.
- For categorized error guidance, see [Troubleshooting common errors](../troubleshooting/error-handling.md).
