using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Provides access to the registered feature catalog.
/// </summary>
public interface IFeatureFlagCatalog
{
    /// <summary>All registered features, in registration order.</summary>
    IReadOnlyList<FeatureFlag> Features { get; }

    /// <summary>Returns the feature with the specified key, or <see langword="null"/> if not registered.</summary>
    FeatureFlag? TryGet(string key);

    /// <summary>Returns <see langword="true"/> and the feature when it is registered.</summary>
    bool TryGet(string key, out FeatureFlag? feature);
}

