namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// A database-related OpenTelemetry span correlated to an Aspire resource and request trace.
/// </summary>
/// <param name="SpanId">64-bit OpenTelemetry span identifier (hex string).</param>
/// <param name="TraceId">128-bit OpenTelemetry trace identifier (hex string).</param>
/// <param name="ServiceName">
/// Aspire resource name from the <c>service.name</c> resource attribute.
/// Falls back to <c>"Unknown Service"</c> when missing.
/// </param>
/// <param name="DbSystem">
/// Database system identifier from the <c>db.system</c> span attribute (e.g. <c>mssql</c>, <c>postgresql</c>).
/// </param>
/// <param name="DbName">
/// Target database name from the <c>db.name</c> span attribute.
/// Falls back to <c>"Unknown Database"</c> when missing.
/// </param>
/// <param name="DbStatement">
/// SQL statement text from the <c>db.statement</c> span attribute.
/// May be pre-sanitized by the instrumentation SDK. <see langword="null"/> when not captured.
/// </param>
/// <param name="PeerAddress">
/// Connection endpoint from <c>server.address</c> and <c>server.port</c> span attributes.
/// </param>
/// <param name="StartTime">UTC start time of the span.</param>
/// <param name="Duration">Elapsed duration of the span.</param>
/// <param name="StatusCode">OpenTelemetry status code for the span.</param>
/// <param name="ErrorMessage">
/// Error message when <see cref="StatusCode"/> is <see cref="SpanStatusCode.Error"/>.
/// </param>
public sealed record CorrelatedSpan(
    string SpanId,
    string TraceId,
    string ServiceName,
    string? DbSystem,
    string? DbName,
    string? DbStatement,
    string? PeerAddress,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    SpanStatusCode StatusCode,
    string? ErrorMessage);
