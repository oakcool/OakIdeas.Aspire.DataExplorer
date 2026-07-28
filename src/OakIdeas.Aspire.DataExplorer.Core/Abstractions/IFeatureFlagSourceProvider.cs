using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

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
