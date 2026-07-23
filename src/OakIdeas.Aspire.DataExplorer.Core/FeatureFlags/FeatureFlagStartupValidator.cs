using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Validates the feature flag catalog and smoke-tests all feature evaluations at application startup.
/// Logs warnings for invalid dependencies and errors for evaluation failures.
/// Does not throw; the application continues running with best-effort flag values.
/// </summary>
public sealed class FeatureFlagStartupValidator(
    IFeatureFlagCatalog catalog,
    IFeatureFlagService featureFlagService,
    ILogger<FeatureFlagStartupValidator> logger) : IHostedService
{
    private readonly IFeatureFlagCatalog _catalog = catalog;
    private readonly IFeatureFlagService _featureFlagService = featureFlagService;
    private readonly ILogger<FeatureFlagStartupValidator> _logger = logger;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ValidateCatalog();
        await SmokeTestEvaluationsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidateCatalog()
    {
        if (_catalog is not FeatureFlagCatalog concreteCatalog)
        {
            _logger.LogDebug("Feature flag catalog dependency validation skipped: catalog is not a {CatalogType}.",
                nameof(FeatureFlagCatalog));
            return;
        }

        var errors = concreteCatalog.ValidateDependencies();
        if (errors.Count == 0)
        {
            _logger.LogDebug("Feature flag catalog dependency validation passed. {FeatureCount} feature(s) registered.",
                _catalog.Features.Count);
            return;
        }

        foreach (var error in errors)
        {
            _logger.LogWarning("Feature flag catalog validation issue: {ValidationError}", error);
        }
    }

    private async Task SmokeTestEvaluationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Smoke-testing {FeatureCount} feature flag evaluation(s) at startup.", _catalog.Features.Count);

        foreach (var feature in _catalog.Features)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var result = await _featureFlagService
                    .EvaluateAsync(feature, FeatureEvaluationContext.Empty, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug(
                    "Feature flag startup evaluation: {Key} = {IsEnabled} (source: {WinningSource})",
                    result.Key, result.IsEnabled, result.WinningSource);

                foreach (var warning in result.Warnings)
                {
                    _logger.LogWarning(
                        "Feature flag '{Key}' produced a startup evaluation warning: {Warning}",
                        feature.Key, warning);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Feature flag '{Key}' threw an unexpected error during startup evaluation.",
                    feature.Key);
            }
        }
    }
}
