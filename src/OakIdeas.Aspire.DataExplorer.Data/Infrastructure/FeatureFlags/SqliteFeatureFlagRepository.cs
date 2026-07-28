using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Data.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

/// <summary>
/// SQLite-backed implementation of <see cref="IFeatureFlagRepository"/>.
/// Keeps a single open connection for the lifetime of the repository so that
/// in-memory connection strings (e.g. <c>Data Source=:memory:</c>) retain their data
/// across calls, and so that file-based databases avoid repeated open/close overhead.
/// </summary>
public sealed class SqliteFeatureFlagRepository : IFeatureFlagRepository, IDisposable
{
    private const string TableName = "FeatureFlags";

    private readonly string _connectionString;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private SqliteConnection? _connection;

    public SqliteFeatureFlagRepository(IOptions<SqliteFeatureFlagOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = ResolveConnectionString(options.Value);
    }

    /// <summary>
    /// Computes the effective connection string from the supplied options, creating the
    /// backing directory when a file-based default path is used.
    /// </summary>
    public static string ResolveConnectionString(SqliteFeatureFlagOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        var directory = string.IsNullOrWhiteSpace(options.DataDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OakIdeas", "DataExplorer")
            : options.DataDirectory;

        Directory.CreateDirectory(directory);

        var dbPath = Path.Combine(directory, "feature-flags.db");
        return $"Data Source={dbPath}";
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                Key TEXT NOT NULL PRIMARY KEY,
                IsEnabled INTEGER NOT NULL,
                Notes TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_FeatureFlags_Key ON {TableName} (Key);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SeedAsync(IEnumerable<FeatureFlag> features, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(features);

        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow.ToString("O");

        foreach (var feature in features)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                INSERT OR IGNORE INTO {TableName} (Key, IsEnabled, Notes, CreatedAt, UpdatedAt, RowVersion)
                VALUES (@Key, @IsEnabled, NULL, @Now, @Now, 0);
                """;
            command.Parameters.AddWithValue("@Key", feature.Key);
            command.Parameters.AddWithValue("@IsEnabled", feature.DefaultEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@Now", now);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeatureFlagRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Key, IsEnabled, Notes, CreatedAt, UpdatedAt, RowVersion FROM {TableName} ORDER BY Key;";

        var results = new List<FeatureFlagRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(ReadRecord(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<FeatureFlagRecord?> TryGetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Key, IsEnabled, Notes, CreatedAt, UpdatedAt, RowVersion FROM {TableName} WHERE Key = @Key;";
        command.Parameters.AddWithValue("@Key", key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadRecord(reader) : null;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string key, bool isEnabled, string? notes, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow.ToString("O");

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {TableName} (Key, IsEnabled, Notes, CreatedAt, UpdatedAt, RowVersion)
            VALUES (@Key, @IsEnabled, @Notes, @Now, @Now, 0)
            ON CONFLICT(Key) DO UPDATE SET
                IsEnabled = excluded.IsEnabled,
                Notes = excluded.Notes,
                UpdatedAt = excluded.UpdatedAt,
                RowVersion = {TableName}.RowVersion + 1;
            """;
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@IsEnabled", isEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);
        command.Parameters.AddWithValue("@Now", now);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FeatureFlagRecord ReadRecord(SqliteDataReader reader) => new(
        Key: reader.GetString(0),
        IsEnabled: reader.GetInt64(1) != 0,
        Notes: reader.IsDBNull(2) ? null : reader.GetString(2),
        CreatedAt: DateTimeOffset.Parse(reader.GetString(3)),
        UpdatedAt: DateTimeOffset.Parse(reader.GetString(4)),
        RowVersion: reader.GetInt64(5));

    private async Task<SqliteConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is null)
            {
                var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                _connection = connection;
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        return _connection;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection?.Dispose();
        _connectionLock.Dispose();
    }
}
