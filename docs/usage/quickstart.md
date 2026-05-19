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
   ```

4. In Object Explorer, browse metadata by:
   - `Schema`
   - `Tables`
   - `Views`
   - `Procedures`
   - `Functions`
   - `Triggers`
5. Select an object to load object details and (when supported) definition text.

## Query window walkthrough

1. Open **Query** in the Data Explorer shell after selecting a database resource.
2. Enter SQL and run it with **Execute** (or `Ctrl+Enter`).
3. Use **Cancel** to stop long-running statements.
4. Review:
   - Returned rows in the results grid
   - Duration, row count, and affected row count status
   - Truncation notice when max row limits are reached
5. For `INSERT`/`UPDATE`/`DELETE`/DDL statements, enable destructive execution confirmation before running.
6. Use in-memory history entries to restore prior queries during the current session.

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
