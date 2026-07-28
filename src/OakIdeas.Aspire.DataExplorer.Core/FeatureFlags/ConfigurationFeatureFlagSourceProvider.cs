using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Feature flag source that reads from the standard .NET <see cref="IConfiguration"/> pipeline.
/// Configuration values are read from the section
/// <c>OakIdeas:Aspire:DataExplorer:FeatureFlags</c>.
/// A missing key means the source has no opinion (not-defined); an invalid value produces a diagnostic.
/// </summary>
public sealed class ConfigurationFeatureFlagSourceProvider(
    IConfiguration configuration,
    ILogger<ConfigurationFeatureFlagSourceProvider> logger) : IFeatureFlagSourceProvider
{
    /// <summary>Configuration section path for feature flags.</summary>
    public const string SectionPath = "OakIdeas:Aspire:DataExplorer:FeatureFlags";

    /// <summary>Source name used in evaluation traces.</summary>
    public const string SourceName = "Configuration";

    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ConfigurationFeatureFlagSourceProvider> _logger = logger;

    /// <inheritdoc />
    public string Name => SourceName;

    /// <inheritdoc />
    public ValueTask<FeatureFlagSourceResult> TryGetAsync(
        FeatureFlag feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(context);

        var configKey = $"{SectionPath}:{feature.Key}";
        var rawValue = _configuration[configKey];

        if (rawValue is null)
        {
            return ValueTask.FromResult(FeatureFlagSourceResult.NotDefined(Name));
        }

        if (bool.TryParse(rawValue, out var boolValue))
        {
            return ValueTask.FromResult(
                boolValue
                    ? FeatureFlagSourceResult.Enabled(Name)
                    : FeatureFlagSourceResult.Disabled(Name));
        }

        _logger.LogWarning(
            "Feature flag '{FeatureKey}' has an invalid configuration value '{RawValue}' at '{ConfigKey}'. Expected 'true' or 'false'.",
            feature.Key, rawValue, configKey);

        return ValueTask.FromResult(
            FeatureFlagSourceResult.Invalid(Name, $"Value '{rawValue}' is not a valid Boolean. Expected 'true' or 'false'."));
    }
}
