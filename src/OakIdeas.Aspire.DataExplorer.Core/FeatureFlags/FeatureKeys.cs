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
