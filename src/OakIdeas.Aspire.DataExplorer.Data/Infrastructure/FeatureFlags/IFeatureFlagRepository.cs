using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

/// <summary>
/// Persists feature flag overrides. Implementations own their storage schema and connection lifetime.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>
    /// Creates the underlying schema if it does not already exist. Safe to call repeatedly.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Idempotently seeds the repository with the supplied catalog features.
    /// Existing records (including manual overrides) are never overwritten.
    /// </summary>
    Task SeedAsync(IEnumerable<FeatureFlag> features, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all persisted feature flag records.
    /// </summary>
    Task<IReadOnlyList<FeatureFlagRecord>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the persisted record for the specified key, or <see langword="null"/> when not present.
    /// </summary>
    Task<FeatureFlagRecord?> TryGetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates the record for the specified key.
    /// </summary>
    Task UpsertAsync(string key, bool isEnabled, string? notes, CancellationToken cancellationToken);
}
