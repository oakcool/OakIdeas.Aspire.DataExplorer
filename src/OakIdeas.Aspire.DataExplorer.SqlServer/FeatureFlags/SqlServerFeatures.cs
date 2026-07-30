using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.FeatureFlags;

/// <summary>
/// The feature catalog for SQL Server-specific feature flags.
/// All flags default to <see langword="true"/> because they represent capabilities that
/// SQL Server actively supports. Each flag declares a <see cref="FeatureFlag.DependsOn"/>
/// link to the corresponding application-level Explorer or Query flag so that disabling
/// the broader feature cascades to the SQL Server-specific sub-feature.
/// </summary>
public static class SqlServerFeatures
{
    /// <summary>SQL Server stored procedure support.</summary>
    public static readonly FeatureFlag StoredProcedures = new()
    {
        Key = SqlServerFeatureKeys.StoredProcedures,
        DisplayName = "SQL Server – Stored Procedures",
        Description = "Browse, inspect, and script stored procedures in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerStoredProcedures],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server function support.</summary>
    public static readonly FeatureFlag Functions = new()
    {
        Key = SqlServerFeatureKeys.Functions,
        DisplayName = "SQL Server – Functions",
        Description = "Browse, inspect, and script user-defined functions in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerFunctions],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server trigger support.</summary>
    public static readonly FeatureFlag Triggers = new()
    {
        Key = SqlServerFeatureKeys.Triggers,
        DisplayName = "SQL Server – Triggers",
        Description = "Browse, inspect, and script triggers in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerTriggers],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server index support.</summary>
    public static readonly FeatureFlag Indexes = new()
    {
        Key = SqlServerFeatureKeys.Indexes,
        DisplayName = "SQL Server – Indexes",
        Description = "View index definitions and statistics for tables in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerIndexes],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server constraint support.</summary>
    public static readonly FeatureFlag Constraints = new()
    {
        Key = SqlServerFeatureKeys.Constraints,
        DisplayName = "SQL Server – Constraints",
        Description = "View check, default, and unique constraint definitions for tables in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerConstraints],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server foreign key support.</summary>
    public static readonly FeatureFlag ForeignKeys = new()
    {
        Key = SqlServerFeatureKeys.ForeignKeys,
        DisplayName = "SQL Server – Foreign Keys",
        Description = "View and script foreign key relationships for tables in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerForeignKeys],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server primary key support.</summary>
    public static readonly FeatureFlag PrimaryKeys = new()
    {
        Key = SqlServerFeatureKeys.PrimaryKeys,
        DisplayName = "SQL Server – Primary Keys",
        Description = "View primary key definitions for tables in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerPrimaryKeys],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server object definition support.</summary>
    public static readonly FeatureFlag ObjectDefinition = new()
    {
        Key = SqlServerFeatureKeys.ObjectDefinition,
        DisplayName = "SQL Server – Object Definition",
        Description = "Retrieve T-SQL source definitions for views, stored procedures, functions, and triggers in a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.ExplorerObjectDefinition],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server execution plan support.</summary>
    public static readonly FeatureFlag ExecutionPlan = new()
    {
        Key = SqlServerFeatureKeys.ExecutionPlan,
        DisplayName = "SQL Server – Execution Plan",
        Description = "Capture and display actual query execution plans for queries run against a SQL Server database.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.GenerallyAvailable,
        DependsOn = [FeatureKeys.QueryExecutionPlan],
        Owner = "SqlServer",
    };

    /// <summary>SQL Server relationship navigation support.</summary>
    public static readonly FeatureFlag RelationshipNavigation = new()
    {
        Key = SqlServerFeatureKeys.RelationshipNavigation,
        DisplayName = "SQL Server – Relationship Navigation",
        Description = "Navigate parent and child records using foreign key relationships discovered from SQL Server system catalogs.",
        Category = FeatureCategory.Provider,
        DefaultEnabled = true,
        Lifecycle = FeatureLifecycle.Preview,
        DependsOn = [FeatureKeys.NavigatorRelationshipAwareNavigator],
        Owner = "SqlServer",
    };

    /// <summary>
    /// Returns all SQL Server-specific feature flags in their canonical order.
    /// </summary>
    public static IReadOnlyList<FeatureFlag> All { get; } =
    [
        StoredProcedures,
        Functions,
        Triggers,
        Indexes,
        Constraints,
        ForeignKeys,
        PrimaryKeys,
        ObjectDefinition,
        ExecutionPlan,
        RelationshipNavigation,
    ];
}
