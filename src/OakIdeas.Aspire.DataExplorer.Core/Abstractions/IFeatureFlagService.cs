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
