using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Evaluates feature flags by walking an ordered list of source providers.
/// Falls back to the catalog default when no source defines the flag.
/// </summary>
public sealed class FeatureFlagService(
    IFeatureFlagCatalog catalog,
    IEnumerable<OrderedSourceProvider> sources,
    IOptions<FeatureFlagOptions> options,
    ILogger<FeatureFlagService> logger) : IFeatureFlagService
{
    private const string CatalogDefaultSourceName = "CatalogDefault";

    private readonly IFeatureFlagCatalog _catalog = catalog;
    private readonly IReadOnlyList<OrderedSourceProvider> _sources = sources
        .OrderBy(static s => s.Priority)
        .ToArray();
    private readonly FeatureFlagOptions _options = options.Value;
    private readonly ILogger<FeatureFlagService> _logger = logger;

    /// <inheritdoc />
    public async ValueTask<FeatureFlagResult> EvaluateAsync(
        FeatureFlag feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(context);

        var trace = new List<FeatureFlagSourceResult>();
        var warnings = new List<string>();

        foreach (var orderedSource in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FeatureFlagSourceResult sourceResult;
            try
            {
                sourceResult = await orderedSource.Provider.TryGetAsync(feature, context, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var reason = $"Source '{orderedSource.Provider.Name}' threw an unhandled exception: {ex.GetType().Name}";
                _logger.LogWarning(ex,
                    "Feature flag source '{SourceName}' threw an exception evaluating '{FeatureKey}': {ExceptionType}",
                    orderedSource.Provider.Name, feature.Key, ex.GetType().Name);

                sourceResult = FeatureFlagSourceResult.FromError(orderedSource.Provider.Name, reason);
            }

            trace.Add(sourceResult);

            switch (sourceResult.Outcome)
            {
                case FeatureFlagSourceOutcome.Enabled:
                    return new FeatureFlagResult
                    {
                        Key = feature.Key,
                        IsEnabled = true,
                        WinningSource = sourceResult.SourceName,
                        UsedCatalogDefault = false,
                        EvaluationTrace = trace.AsReadOnly(),
                        Warnings = warnings.AsReadOnly(),
                    };

                case FeatureFlagSourceOutcome.Disabled:
                    return new FeatureFlagResult
                    {
                        Key = feature.Key,
                        IsEnabled = false,
                        WinningSource = sourceResult.SourceName,
                        UsedCatalogDefault = false,
                        EvaluationTrace = trace.AsReadOnly(),
                        Warnings = warnings.AsReadOnly(),
                    };

                case FeatureFlagSourceOutcome.NotDefined:
                    // Continue to next source.
                    break;

                case FeatureFlagSourceOutcome.SourceUnavailable:
                    warnings.Add($"Source '{sourceResult.SourceName}' was unavailable: {sourceResult.Reason ?? "no reason provided"}.");
                    break;

                case FeatureFlagSourceOutcome.InvalidValue:
                    warnings.Add($"Source '{sourceResult.SourceName}' returned an invalid value for '{feature.Key}': {sourceResult.Reason ?? "no details"}.");
                    break;

                case FeatureFlagSourceOutcome.Error:
                    warnings.Add($"Source '{sourceResult.SourceName}' encountered an error: {sourceResult.Reason ?? "no details"}.");
                    break;
            }
        }

        // No source defined this flag; use the catalog default.
        bool defaultValue = _options.DefaultFailureBehavior == FeatureFlagFailureBehavior.FailClosed
            ? false
            : feature.DefaultEnabled;

        return new FeatureFlagResult
        {
            Key = feature.Key,
            IsEnabled = defaultValue,
            WinningSource = CatalogDefaultSourceName,
            UsedCatalogDefault = true,
            EvaluationTrace = trace.AsReadOnly(),
            Warnings = warnings.AsReadOnly(),
        };
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsEnabledAsync(
        FeatureFlag feature,
        FeatureEvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(feature, context ?? FeatureEvaluationContext.Empty, cancellationToken)
            .ConfigureAwait(false);
        return result.IsEnabled;
    }
}

