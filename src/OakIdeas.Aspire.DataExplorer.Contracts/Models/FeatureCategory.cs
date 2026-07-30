namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Category grouping for feature flags, corresponding to application areas.
/// </summary>
public enum FeatureCategory
{
    Unknown = 0,
    Explorer = 1,
    Query = 2,
    Diagram = 3,
    DataEditing = 4,
    Providers = 5,
    Infrastructure = 6,

    /// <summary>Provider-specific feature capabilities contributed by a database provider.</summary>
    Provider = 7,

    /// <summary>Telemetry and request tracing features.</summary>
    Telemetry = 8,
}
