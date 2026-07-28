namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

/// <summary>
/// Persistence model for a feature flag override stored in SQLite.
/// </summary>
public sealed record FeatureFlagRecord(
    string Key,
    bool IsEnabled,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long RowVersion);
