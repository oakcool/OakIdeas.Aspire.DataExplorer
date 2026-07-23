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

/// <summary>
/// Registration entry for a source provider, including its priority.
/// </summary>
public sealed record SourceProviderRegistration(
    int Priority,
    Type ProviderImplementationType);

/// <summary>
/// Options for the feature flag system.
/// </summary>
public sealed class FeatureFlagOptions
{
    /// <summary>
    /// Behavior when a source fails or is unavailable.
    /// Defaults to <see cref="FeatureFlagFailureBehavior.UseCatalogDefault"/>.
    /// </summary>
    public FeatureFlagFailureBehavior DefaultFailureBehavior { get; set; } = FeatureFlagFailureBehavior.UseCatalogDefault;

    internal List<SourceProviderRegistration> SourceRegistrations { get; } = [];

    internal List<FeatureFlag> CatalogFeatures { get; } = [];

    /// <summary>
    /// Registers a feature flag source provider with the given priority.
    /// Lower priority numbers run first (higher precedence).
    /// </summary>
    public FeatureFlagOptions AddSource(int priority, Type providerType)
    {
        ArgumentNullException.ThrowIfNull(providerType);

        if (!typeof(IFeatureFlagSourceProvider).IsAssignableFrom(providerType))
        {
            throw new ArgumentException(
                $"Provider type '{providerType.FullName}' must implement {nameof(IFeatureFlagSourceProvider)}.",
                nameof(providerType));
        }

        SourceRegistrations.Add(new SourceProviderRegistration(priority, providerType));
        return this;
    }

    /// <summary>
    /// Registers one or more features in the catalog.
    /// Duplicate keys are rejected.
    /// </summary>
    public FeatureFlagOptions AddFeatures(IEnumerable<FeatureFlag> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        foreach (var feature in features)
        {
            CatalogFeatures.Add(feature);
        }
        return this;
    }
}
