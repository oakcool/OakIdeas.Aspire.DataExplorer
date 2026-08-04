using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// The centralized application feature catalog.
/// Contains all registered <see cref="FeatureFlag"/> definitions with their defaults.
/// All existing features default to <see langword="true"/> to preserve current behavior.
/// </summary>
public static class ApplicationFeatures
{
    /// <summary>Object Explorer feature.</summary>
    public static readonly FeatureFlag ObjectExplorer = new()
    {
        Key = FeatureKeys.ExplorerObjectExplorer,
        DisplayName = "Object Explorer",
        Description = "Browse database objects in a tree view by schema, table, view, procedure, function, and trigger.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "Explorer",
    };

    /// <summary>Object Details feature.</summary>
    public static readonly FeatureFlag ObjectDetails = new()
    {
        Key = FeatureKeys.ExplorerObjectDetails,
        DisplayName = "Object Details",
        Description = "View column, index, key, constraint, and definition details for a selected database object.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Query Editor feature.</summary>
    public static readonly FeatureFlag QueryEditor = new()
    {
        Key = FeatureKeys.QueryEditor,
        DisplayName = "Query Editor",
        Description = "Compose and execute ad-hoc SQL queries against the selected database.",
        Category = FeatureCategory.Query,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "Query",
    };

    /// <summary>Auto-Execute feature.</summary>
    public static readonly FeatureFlag QueryAutoExecute = new()
    {
        Key = FeatureKeys.QueryAutoExecute,
        DisplayName = "Auto-Execute",
        Description = "Automatically execute a context-menu script when navigating to the Query page from the Object Explorer.",
        Category = FeatureCategory.Query,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.QueryEditor],
        Owner = "Query",
    };

    /// <summary>Execution Plan feature.</summary>
    public static readonly FeatureFlag QueryExecutionPlan = new()
    {
        Key = FeatureKeys.QueryExecutionPlan,
        DisplayName = "Execution Plan",
        Description = "Include the query execution plan alongside results when supported by the active provider.",
        Category = FeatureCategory.Query,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.QueryEditor],
        Owner = "Query",
    };

    /// <summary>Database Diagram feature.</summary>
    public static readonly FeatureFlag DatabaseDiagram = new()
    {
        Key = FeatureKeys.DiagramDatabaseDiagram,
        DisplayName = "Database Diagram",
        Description = "Visualize entity relationships as an ER diagram for the selected database.",
        Category = FeatureCategory.Diagram,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "Diagram",
    };

    /// <summary>Data Insert feature.</summary>
    public static readonly FeatureFlag DataInsert = new()
    {
        Key = FeatureKeys.DataEditingInsert,
        DisplayName = "Data Insert",
        Description = "Insert new rows into database tables using the inline data editor.",
        Category = FeatureCategory.DataEditing,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "DataEditing",
    };

    /// <summary>Data Update feature.</summary>
    public static readonly FeatureFlag DataUpdate = new()
    {
        Key = FeatureKeys.DataEditingUpdate,
        DisplayName = "Data Update",
        Description = "Modify existing rows in database tables using the inline data editor.",
        Category = FeatureCategory.DataEditing,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "DataEditing",
    };

    /// <summary>Data Delete feature.</summary>
    public static readonly FeatureFlag DataDelete = new()
    {
        Key = FeatureKeys.DataEditingDelete,
        DisplayName = "Data Delete",
        Description = "Remove rows from database tables using the inline data editor.",
        Category = FeatureCategory.DataEditing,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "DataEditing",
    };

    /// <summary>Multiple Databases feature.</summary>
    public static readonly FeatureFlag MultipleDatabases = new()
    {
        Key = FeatureKeys.ProvidersMultipleDatabases,
        DisplayName = "Multiple Databases",
        Description = "Connect to and explore more than one Aspire database resource simultaneously.",
        Category = FeatureCategory.Providers,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        Owner = "Providers",
    };

    /// <summary>Views feature.</summary>
    public static readonly FeatureFlag Views = new()
    {
        Key = FeatureKeys.ExplorerViews,
        DisplayName = "Views",
        Description = "Browse and inspect database views in the Object Explorer tree.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Stored Procedures feature.</summary>
    public static readonly FeatureFlag StoredProcedures = new()
    {
        Key = FeatureKeys.ExplorerStoredProcedures,
        DisplayName = "Stored Procedures",
        Description = "Browse and inspect stored procedures in the Object Explorer tree.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Functions feature.</summary>
    public static readonly FeatureFlag Functions = new()
    {
        Key = FeatureKeys.ExplorerFunctions,
        DisplayName = "Functions",
        Description = "Browse and inspect user-defined functions in the Object Explorer tree.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Triggers feature.</summary>
    public static readonly FeatureFlag Triggers = new()
    {
        Key = FeatureKeys.ExplorerTriggers,
        DisplayName = "Triggers",
        Description = "Browse and inspect triggers in the Object Explorer tree.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Indexes feature.</summary>
    public static readonly FeatureFlag Indexes = new()
    {
        Key = FeatureKeys.ExplorerIndexes,
        DisplayName = "Indexes",
        Description = "View index details for tables in the Object Explorer.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectDetails],
        Owner = "Explorer",
    };

    /// <summary>Constraints feature.</summary>
    public static readonly FeatureFlag Constraints = new()
    {
        Key = FeatureKeys.ExplorerConstraints,
        DisplayName = "Constraints",
        Description = "View constraint details for tables in the Object Explorer.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectDetails],
        Owner = "Explorer",
    };

    /// <summary>Foreign Keys feature.</summary>
    public static readonly FeatureFlag ForeignKeys = new()
    {
        Key = FeatureKeys.ExplorerForeignKeys,
        DisplayName = "Foreign Keys",
        Description = "View foreign key relationships for tables in the Object Explorer.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectDetails],
        Owner = "Explorer",
    };

    /// <summary>Primary Keys feature.</summary>
    public static readonly FeatureFlag PrimaryKeys = new()
    {
        Key = FeatureKeys.ExplorerPrimaryKeys,
        DisplayName = "Primary Keys",
        Description = "View primary key details for tables in the Object Explorer.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectDetails],
        Owner = "Explorer",
    };

    /// <summary>Object Definition feature.</summary>
    public static readonly FeatureFlag ObjectDefinition = new()
    {
        Key = FeatureKeys.ExplorerObjectDefinition,
        DisplayName = "Object Definition",
        Description = "Retrieve and display the source definition of views, stored procedures, functions, and triggers.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Schema and Migrations feature.</summary>
    public static readonly FeatureFlag SchemaMigrations = new()
    {
        Key = FeatureKeys.ExplorerSchemaMigrations,
        DisplayName = "Schema and Migrations",
        Description = "Compare schema and migration state across environments and inspect migration scripts before execution.",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = false,
        Lifecycle = FeatureLifecycle.Preview,
        DependsOn = [FeatureKeys.ExplorerObjectExplorer],
        Owner = "Explorer",
    };

    /// <summary>Request-to-Database Trace feature.</summary>
    public static readonly FeatureFlag RequestTrace = new()
    {
        Key = FeatureKeys.TelemetryRequestTrace,
        DisplayName = "Request-to-Database Trace",
        Description = "Correlate Aspire application traces with SQL statements, transactions, and database activity produced by each request.",
        Category = FeatureCategory.Telemetry,
        DefaultEnabled = false,
        Lifecycle = FeatureLifecycle.Preview,
        Owner = "Telemetry",
    };

    /// <summary>Data Change Timeline feature.</summary>
    public static readonly FeatureFlag DataChangeTimeline = new()
    {
        Key = FeatureKeys.TimelineDataChangeTimeline,
        DisplayName = "Data Change Timeline",
        Description = "Capture and display inserts, updates, and deletes that occur while exercising a development workflow. Supports pause, resume, filtering, and export.",
        Category = FeatureCategory.Telemetry,
        DefaultEnabled = false,
        Lifecycle = FeatureLifecycle.Preview,
        Owner = "Timeline",
    };

    /// <summary>Relationship-Aware Data Navigator feature.</summary>
    public static readonly FeatureFlag RelationshipAwareNavigator = new()
    {
        Key = FeatureKeys.NavigatorRelationshipAwareNavigator,
        DisplayName = "Relationship-Aware Data Navigator",
        Description = "Navigate from a record to its parent, child, and many-to-many related records without manually writing joins. Shows related-record counts, generates relationship queries, and visualizes delete impact.",
        Category = FeatureCategory.Navigator,
        DefaultEnabled = false,
        Lifecycle = FeatureLifecycle.Preview,
        Owner = "Navigator",
    };

    /// <summary>Test Data Scenario Builder feature.</summary>
    public static readonly FeatureFlag TestDataScenarioBuilder = new()
    {
        Key = FeatureKeys.ScenarioBuilderTestDataScenarioBuilder,
        DisplayName = "Test Data Scenario Builder",
        Description = "Create, edit, and execute reusable deterministic data scenarios that insert related records in dependency order. Supports fixed values, generated values, references, and repeatability seeds.",
        Category = FeatureCategory.Scenarios,
        DefaultEnabled = false,
        Lifecycle = FeatureLifecycle.Preview,
        Owner = "ScenarioBuilder",
    };

    /// <summary>
    /// Returns all features defined in the application catalog, in their canonical order.
    /// </summary>
    public static IReadOnlyList<FeatureFlag> All { get; } =
    [
        ObjectExplorer,
        ObjectDetails,
        Views,
        StoredProcedures,
        Functions,
        Triggers,
        Indexes,
        Constraints,
        ForeignKeys,
        PrimaryKeys,
        ObjectDefinition,
        SchemaMigrations,
        QueryEditor,
        QueryAutoExecute,
        QueryExecutionPlan,
        DatabaseDiagram,
        DataInsert,
        DataUpdate,
        DataDelete,
        MultipleDatabases,
        RequestTrace,
        DataChangeTimeline,
        RelationshipAwareNavigator,
        TestDataScenarioBuilder,
    ];
}
