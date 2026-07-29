using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Manages the ingestion and correlated querying of database-related OpenTelemetry spans.
/// Implementations must be thread-safe; the service is registered as a singleton.
/// </summary>
public interface ITraceCorrelationService
{
    /// <summary>
    /// Ingests a correlated span into the trace store.
    /// Provider-specific <see cref="ITraceEnrichmentProvider"/> implementations may enrich
    /// the span before storage.
    /// </summary>
    /// <param name="span">The span to ingest. Must not be <see langword="null"/>.</param>
    void IngestSpan(CorrelatedSpan span);

    /// <summary>
    /// Queries the trace store using the provided filters.
    /// Returns spans in descending start-time order.
    /// </summary>
    /// <param name="request">The query filters. Pass a default instance to return all spans.</param>
    /// <returns>A <see cref="TraceQueryResponse"/> containing matching spans.</returns>
    TraceQueryResponse Query(TraceQueryRequest request);

    /// <summary>
    /// Removes all ingested spans from the trace store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns the current count of ingested spans.
    /// </summary>
    int SpanCount { get; }

    /// <summary>
    /// Returns the distinct service names currently in the store, sorted ascending.
    /// </summary>
    IReadOnlyList<string> ServiceNames { get; }

    /// <summary>
    /// Returns the distinct database names currently in the store, sorted ascending.
    /// </summary>
    IReadOnlyList<string> DatabaseNames { get; }
}
