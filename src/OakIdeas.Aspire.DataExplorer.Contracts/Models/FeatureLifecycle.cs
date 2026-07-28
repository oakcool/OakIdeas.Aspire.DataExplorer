namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Lifecycle state for a feature flag. Metadata only in this release; future phases may enforce runtime behavior.
/// </summary>
public enum FeatureLifecycle
{
    Planned = 0,
    Development = 1,
    Preview = 2,
    GenerallyAvailable = 3,
    Deprecated = 4,
    Retired = 5,
}
