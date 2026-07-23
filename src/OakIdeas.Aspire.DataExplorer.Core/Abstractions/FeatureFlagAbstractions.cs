using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Evaluates feature flags through the configured source pipeline.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Evaluates the specified feature for the given context, returning a rich result
    /// that includes the effective value, winning source, and evaluation trace.
    /// </summary>
    ValueTask<FeatureFlagResult> EvaluateAsync(
        FeatureFlag feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience method that returns <see langword="true"/> when the feature is enabled.
    /// </summary>
    ValueTask<bool> IsEnabledAsync(
        FeatureFlag feature,
        FeatureEvaluationContext? context = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies flag values from a single source (configuration, database, etc.).
/// Implementations must be registered with an explicit priority to define source precedence.
/// </summary>
public interface IFeatureFlagSourceProvider
{
    /// <summary>Stable, unique source identifier used in evaluation traces and diagnostics.</summary>
    string Name { get; }

    /// <summary>
    /// Attempts to retrieve the flag value for the specified feature and context.
    /// Return <see cref="FeatureFlagSourceResult.NotDefined(string)"/> when this source has no opinion;
    /// the pipeline will continue to lower-priority sources.
    /// </summary>
    ValueTask<FeatureFlagSourceResult> TryGetAsync(
        FeatureFlag feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default);
}

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
