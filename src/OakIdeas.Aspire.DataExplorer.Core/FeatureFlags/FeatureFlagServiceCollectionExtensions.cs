using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// Extension methods for registering the feature flag system with the service container.
/// </summary>
public static class FeatureFlagServiceCollectionExtensions
{
    /// <summary>
    /// Default priority for the configuration source.
    /// </summary>
    public const int ConfigurationSourcePriority = 200;

    /// <summary>
    /// Registers the feature flag system with default settings and all application features enabled.
    /// Call <see cref="AddConfigurationFeatureFlagSource"/> to add the configuration source.
    /// </summary>
    public static FeatureFlagBuilder AddFeatureFlags(
        this IServiceCollection services,
        Action<FeatureFlagOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();

        services.AddOptions<FeatureFlagOptions>();

        if (configure is not null)
        {
            services.Configure<FeatureFlagOptions>(configure);
        }

        // Register all application features by default.
        services.Configure<FeatureFlagOptions>(opts =>
        {
            foreach (var feature in ApplicationFeatures.All)
            {
                if (!opts.CatalogFeatures.Any(f =>
                    string.Equals(f.Key, feature.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    opts.CatalogFeatures.Add(feature);
                }
            }
        });

        // Register the catalog using the configured features plus any contributor features.
        services.TryAddSingleton<IFeatureFlagCatalog>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FeatureFlagOptions>>().Value;
            var contributors = sp.GetServices<IFeatureFlagContributor>();
            var features = new List<FeatureFlag>(options.CatalogFeatures);

            foreach (var contributor in contributors)
            {
                foreach (var feature in contributor.GetFeatures())
                {
                    if (!features.Any(f =>
                        string.Equals(f.Key, feature.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        features.Add(feature);
                    }
                }
            }

            return new FeatureFlagCatalog(features);
        });

        services.TryAddSingleton<IFeatureFlagService, FeatureFlagService>();

        return new FeatureFlagBuilder(services);
    }

    /// <summary>
    /// Adds the application configuration source to the feature flag pipeline
    /// at <see cref="ConfigurationSourcePriority"/> (200).
    /// </summary>
    public static FeatureFlagBuilder AddConfigurationFeatureFlagSource(
        this FeatureFlagBuilder builder,
        int priority = ConfigurationSourcePriority)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<ConfigurationFeatureFlagSourceProvider>();
        builder.Services.Configure<FeatureFlagOptions>(opts =>
            opts.AddSource(priority, typeof(ConfigurationFeatureFlagSourceProvider)));

        // Register the ordered provider wrapper that the FeatureFlagService will consume.
        builder.Services.AddSingleton<OrderedSourceProvider>(sp =>
        {
            var provider = sp.GetRequiredService<ConfigurationFeatureFlagSourceProvider>();
            return new OrderedSourceProvider(priority, provider);
        });

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IFeatureFlagContributor"/> that will contribute additional
    /// <see cref="FeatureFlag"/> definitions to the catalog at startup.
    /// Use this in provider or feature-area projects to register provider-specific flags
    /// without coupling to the core catalog.
    /// </summary>
    /// <typeparam name="T">The contributor type to register as a singleton.</typeparam>
    public static FeatureFlagBuilder AddFeatureContributor<T>(this FeatureFlagBuilder builder)
        where T : class, IFeatureFlagContributor
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IFeatureFlagContributor, T>();
        return builder;
    }

    /// <summary>
    /// Registers the <see cref="FeatureFlagStartupValidator"/> hosted service, which validates the
    /// feature catalog and smoke-tests all flag evaluations at application startup.
    /// </summary>
    public static FeatureFlagBuilder AddStartupValidation(this FeatureFlagBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHostedService<FeatureFlagStartupValidator>();
        return builder;
    }
}
