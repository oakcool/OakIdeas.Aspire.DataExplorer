# Usage Quickstart

1. Run the AppHost.
2. Open the DataExplorer web app from Aspire dashboard.
3. Resolve `IExplorerService` and call `GetAvailableDatabasesAsync(cancellationToken)` to list discovered database resources.
4. Call `SelectDatabaseAsync(resourceId, cancellationToken)` and then `GetDatabaseMetadataAsync(cancellationToken)` to populate explorer metadata.
5. Use `RefreshDatabaseMetadataAsync(cancellationToken)` and `GetObjectDefinitionAsync(objectId, objectType, cancellationToken)` for refresh and definition workflows.
6. In the Object Explorer pane, browse metadata as a tree grouped by `Schema -> Tables/Views/Procedures/Functions/Triggers -> Objects` and use the refresh button to reload metadata.
7. Object nodes include type icons (table, view, procedure, function, trigger) and support selection for detail navigation workflows.
8. Use Dashboard, Explorer, Table, and Query pages against the selected database context.
