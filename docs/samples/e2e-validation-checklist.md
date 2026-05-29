# E2E Validation Checklist (Issue #25)

## Sample database setup

- [x] Apply `docs/samples/test-database-setup.sql` to a local SQL Server instance.
- [x] Validate schemas exist: `dbo`, `test`, `sample`.
- [x] Validate representative objects exist:
  - [x] Tables, views, procedures, functions, triggers
  - [x] PK/FK/check/default/unique constraints
  - [x] Composite and unique indexes

## Automated integration validation

- [x] Metadata discovery workflow test: `MetadataDiscoveryWorkflow_LoadsAllMetadataObjectTypes`
- [x] Refresh/cache invalidation workflow test: `RefreshWorkflow_RefreshInvalidatesCacheAndLoadsUpdatedMetadata`
- [x] Definition retrieval workflow test: `DefinitionWorkflow_DefinitionRetrievalReturnsSqlDefinition`
- [x] Error recovery workflow test: `ErrorRecoveryWorkflow_RecoversFromDiscoveryFailureAfterRefresh`

Run:

```bash
dotnet test src/tests/OakIdeas.Aspire.DataExplorer.IntegrationTests/OakIdeas.Aspire.DataExplorer.IntegrationTests.csproj
```

## Manual end-to-end walkthrough (Aspire sample + DataExplorer)

1. Start sample AppHost:
   - `dotnet run --project samples/OakIdeas.Aspire.DataExplorer.Sample.AppHost`
2. Start DataExplorer AppHost in another terminal:
   - `dotnet run --project src/OakIdeas.Aspire.DataExplorer.AppHost`
3. In Aspire dashboard:
   - Confirm SQL resource is discovered and available.
   - Open DataExplorer and select the validation database resource.
   - Verify object explorer renders schemas/tables/views/procedures/functions/triggers.
   - Verify metadata leaf formatting uses parenthetical detail style:
     - `Id (PK, identity, int(4), not null)`
     - `PK_TodoItems (Clustered)`
     - `@StatusId (tinyint, Input, Has default)`
   - Verify metadata icons are semantic:
     - common columns use `ViewColumns`
     - PK/identity columns use `Key`
     - FK columns use `Link`
     - parameters use `AtSymbol`
   - Open table metadata and verify columns, nullability, keys, relationships, indexes, constraints.
   - Verify scrollbar styling is consistent across Object Explorer, Explorer details, Query results, and Execution Plan tab.
   - Trigger refresh and verify updates appear.
   - Validate graceful errors for invalid/unavailable selections.

## Sign-off

- [x] Build passes: `dotnet build OakIdeas.Aspire.DataExplorer.sln`
- [x] Tests pass: `dotnet test OakIdeas.Aspire.DataExplorer.sln`
- [x] E2E validation assets added (SQL setup + workflow checklist)
