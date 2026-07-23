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
}

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

    /// <summary>
    /// Returns all features defined in the application catalog, in their canonical order.
    /// </summary>
    public static IReadOnlyList<FeatureFlag> All { get; } =
    [
        ObjectExplorer,
        ObjectDetails,
        QueryEditor,
        QueryAutoExecute,
        QueryExecutionPlan,
        DatabaseDiagram,
        DataInsert,
        DataUpdate,
        DataDelete,
        MultipleDatabases,
    ];
}
