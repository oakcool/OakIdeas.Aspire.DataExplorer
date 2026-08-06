using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Stable string keys for all registered application features.
/// Use these constants when evaluating flags or registering catalog entries.
/// </summary>
public static class FeatureKeys
{
    /// <summary>Object Explorer: browse database objects in the tree view.</summary>
    public const string ExplorerObjectExplorer = "Explorer.ObjectExplorer";

    /// <summary>Object Details: view column, index, key, and definition details for a selected object.</summary>
    public const string ExplorerObjectDetails = "Explorer.ObjectDetails";

    /// <summary>Query Editor: compose and execute SQL queries.</summary>
    public const string QueryEditor = "Query.Editor";

    /// <summary>Auto-Execute: automatically run a script when navigating to the Query page from the explorer.</summary>
    public const string QueryAutoExecute = "Query.AutoExecute";

    /// <summary>Execution Plan: include the query execution plan alongside query results.</summary>
    public const string QueryExecutionPlan = "Query.ExecutionPlan";

    /// <summary>Database Diagram: visualize entity relationships as an ER diagram.</summary>
    public const string DiagramDatabaseDiagram = "Diagram.DatabaseDiagram";

    /// <summary>Data Insert: insert new rows into database tables.</summary>
    public const string DataEditingInsert = "DataEditing.Insert";

    /// <summary>Data Update: modify existing rows in database tables.</summary>
    public const string DataEditingUpdate = "DataEditing.Update";

    /// <summary>Data Delete: remove rows from database tables.</summary>
    public const string DataEditingDelete = "DataEditing.Delete";

    /// <summary>Multiple Databases: connect to and explore more than one database resource simultaneously.</summary>
    public const string ProvidersMultipleDatabases = "Providers.MultipleDatabases";

    /// <summary>Schema and Migrations: compare migration/model/schema state and inspect migration scripts.</summary>
    public const string ExplorerSchemaMigrations = "Explorer.SchemaMigrations";

    /// <summary>Views: browse and inspect database views in the Object Explorer tree.</summary>
    public const string ExplorerViews = "Explorer.Views";

    /// <summary>Stored Procedures: browse and inspect stored procedures in the Object Explorer tree.</summary>
    public const string ExplorerStoredProcedures = "Explorer.StoredProcedures";

    /// <summary>Functions: browse and inspect user-defined functions in the Object Explorer tree.</summary>
    public const string ExplorerFunctions = "Explorer.Functions";

    /// <summary>Triggers: browse and inspect triggers in the Object Explorer tree.</summary>
    public const string ExplorerTriggers = "Explorer.Triggers";

    /// <summary>Indexes: view index details for tables in the Object Explorer.</summary>
    public const string ExplorerIndexes = "Explorer.Indexes";

    /// <summary>Constraints: view constraint details for tables in the Object Explorer.</summary>
    public const string ExplorerConstraints = "Explorer.Constraints";

    /// <summary>Foreign Keys: view foreign key relationships for tables in the Object Explorer.</summary>
    public const string ExplorerForeignKeys = "Explorer.ForeignKeys";

    /// <summary>Primary Keys: view primary key details for tables in the Object Explorer.</summary>
    public const string ExplorerPrimaryKeys = "Explorer.PrimaryKeys";

    /// <summary>Object Definition: retrieve and display the source definition of views, procedures, functions, and triggers.</summary>
    public const string ExplorerObjectDefinition = "Explorer.ObjectDefinition";

    /// <summary>Request-to-Database Trace: correlate Aspire traces with SQL operations produced by each request.</summary>
    public const string TelemetryRequestTrace = "Telemetry.RequestTrace";

    /// <summary>Data Change Timeline: capture and display inserts, updates, and deletes that occur during a development session.</summary>
    public const string TimelineDataChangeTimeline = "Timeline.DataChangeTimeline";

    /// <summary>Relationship-Aware Data Navigator: navigate parent, child, and many-to-many related records without manually writing joins.</summary>
    public const string NavigatorRelationshipAwareNavigator = "Navigator.RelationshipAwareNavigator";

    /// <summary>Test Data Scenario Builder: create, edit, and execute deterministic data scenarios with dependency-ordered inserts.</summary>
    public const string ScenarioBuilderTestDataScenarioBuilder = "ScenarioBuilder.TestDataScenarioBuilder";

    /// <summary>Database Snapshots: create named restore points, compare current state, and restore the database to a previous state.</summary>
    public const string SnapshotsDatabaseSnapshots = "Snapshots.DatabaseSnapshots";

    /// <summary>Query Performance Workspace: surface slow, frequent, expensive, and regressing queries with execution metrics and plan integration.</summary>
    public const string PerformanceQueryPerformanceWorkspace = "Performance.QueryPerformanceWorkspace";
}
