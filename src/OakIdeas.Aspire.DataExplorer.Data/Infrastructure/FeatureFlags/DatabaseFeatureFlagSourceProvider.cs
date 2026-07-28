using Microsoft.Extensions.Logging;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

/// <summary>
/// Feature flag source that reads overrides from a SQLite-backed <see cref="IFeatureFlagRepository"/>.
/// Lazily initializes the schema and seeds catalog defaults on first use so that application startup
/// does not pay the cost unless a feature flag is actually evaluated.
/// </summary>
public sealed class DatabaseFeatureFlagSourceProvider(
    IFeatureFlagRepository repository,
    IFeatureFlagCatalog catalog,
    ILogger<DatabaseFeatureFlagSourceProvider> logger) : IFeatureFlagSourceProvider
{
    /// <summary>Source name used in evaluation traces.</summary>
    public const string SourceName = "Database";

    private readonly IFeatureFlagRepository _repository = repository;
    private readonly IFeatureFlagCatalog _catalog = catalog;
    private readonly ILogger<DatabaseFeatureFlagSourceProvider> _logger = logger;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    /// <inheritdoc />
    public string Name => SourceName;

    /// <inheritdoc />
    public async ValueTask<FeatureFlagSourceResult> TryGetAsync(
        FeatureFlag feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            var record = await _repository.TryGetAsync(feature.Key, cancellationToken).ConfigureAwait(false);

            if (record is null)
            {
                return FeatureFlagSourceResult.NotDefined(Name);
            }

            return record.IsEnabled
                ? FeatureFlagSourceResult.Enabled(Name)
                : FeatureFlagSourceResult.Disabled(Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Database feature flag source failed while evaluating '{FeatureKey}': {ExceptionType}",
                feature.Key, ex.GetType().Name);
            return FeatureFlagSourceResult.Unavailable(Name, $"Database source error: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Ensures the schema is created and catalog defaults are seeded exactly once, in a thread-safe manner.
    /// </summary>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await _repository.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await _repository.SeedAsync(_catalog.Features, cancellationToken).ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
