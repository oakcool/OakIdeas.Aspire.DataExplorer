namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Filters for querying correlated spans from the in-memory trace store.
/// All filter parameters are optional; omitting a parameter includes all spans for that dimension.
/// </summary>
/// <param name="TraceId">When set, returns only spans matching this trace identifier.</param>
/// <param name="ServiceName">When set, returns only spans from the specified service.</param>
/// <param name="DbName">When set, returns only spans targeting the specified database.</param>
/// <param name="StatusCode">When set, returns only spans with the specified status.</param>
/// <param name="MinDurationMs">When set, returns only spans with duration at or above this threshold in milliseconds.</param>
/// <param name="Since">When set, returns only spans that started at or after this UTC timestamp.</param>
/// <param name="MaxSpans">Maximum number of spans to return. Defaults to 500 when <see langword="null"/>.</param>
public sealed record TraceQueryRequest(
    string? TraceId = null,
    string? ServiceName = null,
    string? DbName = null,
    SpanStatusCode? StatusCode = null,
    double? MinDurationMs = null,
    DateTimeOffset? Since = null,
    int? MaxSpans = null);
