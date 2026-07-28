using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Contributes a set of <see cref="FeatureFlag"/> definitions to the application feature catalog.
/// Implement this interface in a provider or feature area project to register feature flags
/// without coupling the provider to the core catalog.
/// Register implementations via
/// <see cref="OakIdeas.Aspire.DataExplorer.Core.FeatureFlags.FeatureFlagBuilder.AddFeatureContributor{T}"/>.
/// </summary>
public interface IFeatureFlagContributor
{
    /// <summary>
    /// Returns the set of feature flags contributed by this implementor.
    /// Keys must be unique across all contributors and the base catalog.
    /// </summary>
    IReadOnlyList<FeatureFlag> GetFeatures();
}
