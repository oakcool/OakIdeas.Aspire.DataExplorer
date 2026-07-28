using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Data.FeatureFlags;

/// <summary>
/// Extension methods for registering the SQLite-backed database feature flag source.
/// </summary>
public static class DatabaseFeatureFlagServiceCollectionExtensions
{
    /// <summary>
    /// Default priority for the database source. Sits in the "remote / database" band (100–199),
    /// giving it higher precedence than the configuration source (200) but lower precedence than
    /// any future emergency override band (0–99).
    /// </summary>
    public const int DatabaseSourcePriority = 150;

    /// <summary>
    /// Adds the SQLite-backed database feature flag source to the pipeline at
    /// <paramref name="priority"/> (defaults to <see cref="DatabaseSourcePriority"/>, 150).
    /// </summary>
    public static FeatureFlagBuilder AddDatabaseFeatureFlagSource(
        this FeatureFlagBuilder builder,
        int priority = DatabaseSourcePriority,
        Action<SqliteFeatureFlagOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<SqliteFeatureFlagOptions>();
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.TryAddSingleton<IFeatureFlagRepository, SqliteFeatureFlagRepository>();
        builder.Services.TryAddSingleton<DatabaseFeatureFlagSourceProvider>();

        builder.Services.Configure<FeatureFlagOptions>(opts =>
            opts.AddSource(priority, typeof(DatabaseFeatureFlagSourceProvider)));

        // Register the ordered provider wrapper that the FeatureFlagService will consume.
        builder.Services.AddSingleton<OrderedSourceProvider>(sp =>
        {
            var provider = sp.GetRequiredService<DatabaseFeatureFlagSourceProvider>();
            return new OrderedSourceProvider(priority, provider);
        });

        return builder;
    }
}
