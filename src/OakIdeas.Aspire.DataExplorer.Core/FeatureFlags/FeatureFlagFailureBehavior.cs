using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Behavior when a source fails, times out, or returns an error.
/// </summary>
public enum FeatureFlagFailureBehavior
{
    /// <summary>Fall back to the catalog default when a source fails. This is the default.</summary>
    UseCatalogDefault = 0,

    /// <summary>Treat the flag as disabled when all sources fail and no catalog default applies.</summary>
    FailClosed = 1,
}
