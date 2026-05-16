using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class InMemoryMetadataCache(
    IOptions<MetadataAggregationOptions> options) : IMetadataCache
{
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.CacheTtlMinutes));
    private readonly Dictionary<(string ResourceId, string DatabaseName), CacheEntry> _entries = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<DatabaseMetadataRoot?> GetAsync(
        string resourceId,
        string databaseName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey(resourceId, databaseName);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                return null;
            }

            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _entries.Remove(key);
                return null;
            }

            return entry.Metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetAsync(
        string resourceId,
        string databaseName,
        DatabaseMetadataRoot metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(metadata);
        var key = CreateKey(resourceId, databaseName);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _entries[key] = new CacheEntry(metadata, DateTimeOffset.UtcNow.Add(_ttl));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task InvalidateAsync(
        string resourceId,
        string databaseName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey(resourceId, databaseName);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _entries.Remove(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static (string ResourceId, string DatabaseName) CreateKey(string resourceId, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ArgumentException("Resource ID is required.", nameof(resourceId));
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("Database name is required.", nameof(databaseName));
        }

        return (resourceId.Trim(), databaseName.Trim());
    }

    private sealed record CacheEntry(
        DatabaseMetadataRoot Metadata,
        DateTimeOffset ExpiresAt);
}
