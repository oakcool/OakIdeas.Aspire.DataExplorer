# Usage Quickstart

1. Run the AppHost.
2. Open the DataExplorer web app from Aspire dashboard.
3. Resolve `IExplorerService` and call `GetAvailableDatabasesAsync(cancellationToken)` to list discovered database resources.
4. Call `SelectDatabaseAsync(resourceId, cancellationToken)` and then `GetDatabaseMetadataAsync(cancellationToken)` to populate explorer metadata.
5. Use `RefreshDatabaseMetadataAsync(cancellationToken)` and `GetObjectDefinitionAsync(objectId, objectType, cancellationToken)` for refresh and definition workflows.
6. Use Dashboard, Explorer, Table, and Query pages against the selected database context.
