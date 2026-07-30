namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Filters for querying the data change event store within a capture session.
/// Unset properties are not applied as filters; all events are returned when all are <see langword="null"/>.
/// </summary>
/// <param name="TableName">Filter events to a specific table name (case-insensitive).</param>
/// <param name="SchemaName">Filter events to a specific schema name (case-insensitive).</param>
/// <param name="Operation">Filter events to a specific operation type.</param>
/// <param name="TraceId">Filter events correlated with a specific trace ID.</param>
/// <param name="TransactionId">Filter events associated with a specific transaction ID.</param>
/// <param name="Since">Return only events captured at or after this UTC timestamp.</param>
/// <param name="MaxEvents">Maximum number of events to return. Defaults to 500 when not specified.</param>
public sealed record DataChangeQueryRequest(
    string? TableName = null,
    string? SchemaName = null,
    DataChangeOperation? Operation = null,
    string? TraceId = null,
    string? TransactionId = null,
    DateTimeOffset? Since = null,
    int? MaxEvents = null);
