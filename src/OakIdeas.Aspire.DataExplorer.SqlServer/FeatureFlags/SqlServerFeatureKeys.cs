namespace OakIdeas.Aspire.DataExplorer.SqlServer.FeatureFlags;

/// <summary>
/// Stable string keys for SQL Server-specific feature flags contributed by
/// <see cref="SqlServerFeatureContributor"/>.
/// These keys represent capabilities that are specific to the SQL Server provider and may
/// not exist in other database providers.
/// </summary>
public static class SqlServerFeatureKeys
{
    /// <summary>SQL Server stored procedure support: browse, inspect, and script stored procedures.</summary>
    public const string StoredProcedures = "SqlServer.StoredProcedures";

    /// <summary>SQL Server function support: browse, inspect, and script user-defined functions.</summary>
    public const string Functions = "SqlServer.Functions";

    /// <summary>SQL Server trigger support: browse, inspect, and script triggers.</summary>
    public const string Triggers = "SqlServer.Triggers";

    /// <summary>SQL Server index support: view index definitions and statistics for tables.</summary>
    public const string Indexes = "SqlServer.Indexes";

    /// <summary>SQL Server constraint support: view check, default, and unique constraints for tables.</summary>
    public const string Constraints = "SqlServer.Constraints";

    /// <summary>SQL Server foreign key support: view and script foreign key relationships.</summary>
    public const string ForeignKeys = "SqlServer.ForeignKeys";

    /// <summary>SQL Server primary key support: view primary key definitions for tables.</summary>
    public const string PrimaryKeys = "SqlServer.PrimaryKeys";

    /// <summary>SQL Server object definition support: retrieve TSQL source definitions for views, procedures, functions, and triggers.</summary>
    public const string ObjectDefinition = "SqlServer.ObjectDefinition";

    /// <summary>SQL Server execution plan support: capture and display query execution plans.</summary>
    public const string ExecutionPlan = "SqlServer.ExecutionPlan";

    /// <summary>SQL Server relationship navigation support: navigate parent and child records using foreign key relationships.</summary>
    public const string RelationshipNavigation = "SqlServer.RelationshipNavigation";
}
