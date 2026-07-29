using System.Collections.Concurrent;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// Thread-safe, bounded in-memory implementation of <see cref="ITraceCorrelationService"/>.
/// Oldest spans are evicted when the store reaches <see cref="MaxSpans"/>.
/// Intended for development-time use only; state is not persisted across restarts.
/// </summary>
public sealed class InMemoryTraceCorrelationService : ITraceCorrelationService
{
    /// <summary>Default maximum number of spans to retain.</summary>
    public const int DefaultMaxSpans = 10_000;

    private readonly int _maxSpans;
    private readonly IReadOnlyList<ITraceEnrichmentProvider> _enrichmentProviders;
    private readonly LinkedList<CorrelatedSpan> _spans = new();
    private readonly Lock _lock = new();

    /// <summary>
    /// Initialises a new instance with optional enrichment providers and capacity.
    /// </summary>
    /// <param name="enrichmentProviders">
    /// Provider-specific enrichment implementations. May be empty.
    /// </param>
    /// <param name="maxSpans">
    /// Maximum number of spans to retain. Defaults to <see cref="DefaultMaxSpans"/>.
    /// </param>
    public InMemoryTraceCorrelationService(
        IEnumerable<ITraceEnrichmentProvider> enrichmentProviders,
        int maxSpans = DefaultMaxSpans)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSpans, 1);
        _maxSpans = maxSpans;
        _enrichmentProviders = enrichmentProviders.ToArray();
    }

    /// <inheritdoc />
    public int SpanCount
    {
        get
        {
            lock (_lock) { return _spans.Count; }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ServiceNames
    {
        get
        {
            lock (_lock)
            {
                return _spans
                    .Select(s => s.ServiceName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> DatabaseNames
    {
        get
        {
            lock (_lock)
            {
                return _spans
                    .Where(s => s.DbName is not null)
                    .Select(s => s.DbName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void IngestSpan(CorrelatedSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);

        var enriched = ApplyEnrichment(span);

        lock (_lock)
        {
            _spans.AddLast(enriched);

            while (_spans.Count > _maxSpans)
            {
                _spans.RemoveFirst();
            }
        }
    }

    /// <inheritdoc />
    public TraceQueryResponse Query(TraceQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CorrelatedSpan[] snapshot;
        lock (_lock) { snapshot = [.. _spans]; }

        var filtered = (IEnumerable<CorrelatedSpan>)snapshot;

        if (request.TraceId is not null)
        {
            filtered = filtered.Where(s =>
                string.Equals(s.TraceId, request.TraceId, StringComparison.OrdinalIgnoreCase));
        }

        if (request.ServiceName is not null)
        {
            filtered = filtered.Where(s =>
                string.Equals(s.ServiceName, request.ServiceName, StringComparison.OrdinalIgnoreCase));
        }

        if (request.DbName is not null)
        {
            filtered = filtered.Where(s =>
                string.Equals(s.DbName, request.DbName, StringComparison.OrdinalIgnoreCase));
        }

        if (request.StatusCode is not null)
        {
            filtered = filtered.Where(s => s.StatusCode == request.StatusCode.Value);
        }

        if (request.MinDurationMs is not null)
        {
            var threshold = TimeSpan.FromMilliseconds(request.MinDurationMs.Value);
            filtered = filtered.Where(s => s.Duration >= threshold);
        }

        if (request.Since is not null)
        {
            filtered = filtered.Where(s => s.StartTime >= request.Since.Value);
        }

        // Materialise after filtering so total count is accurate.
        var all = filtered.OrderByDescending(s => s.StartTime).ToArray();
        var totalCount = all.Length;

        var cap = request.MaxSpans ?? 500;
        var truncated = totalCount > cap;
        var page = truncated ? all.AsSpan(0, cap).ToArray() : all;

        return new TraceQueryResponse(page, totalCount, truncated);
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock) { _spans.Clear(); }
    }

    private CorrelatedSpan ApplyEnrichment(CorrelatedSpan span)
    {
        foreach (var provider in _enrichmentProviders)
        {
            span = provider.Enrich(span);
        }

        return span;
    }
}
