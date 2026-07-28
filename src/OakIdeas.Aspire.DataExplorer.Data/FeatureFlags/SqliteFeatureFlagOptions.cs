namespace OakIdeas.Aspire.DataExplorer.Data.FeatureFlags;

/// <summary>
/// Options controlling the SQLite-backed feature flag store.
/// The store is intentionally independent from any SQL Server (or other provider) connection
/// to avoid a circular dependency between feature flag evaluation and provider initialization.
/// </summary>
public sealed class SqliteFeatureFlagOptions
{
    /// <summary>
    /// Explicit SQLite connection string. When empty, a connection string is computed from
    /// <see cref="DataDirectory"/> (or the local application data folder) pointing at
    /// <c>feature-flags.db</c>.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Directory used to compute the default connection string when <see cref="ConnectionString"/> is empty.
    /// Defaults to <c>%LocalAppData%/OakIdeas/DataExplorer</c>.
    /// </summary>
    public string? DataDirectory { get; set; }
}
