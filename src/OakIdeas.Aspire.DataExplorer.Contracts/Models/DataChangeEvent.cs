namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// A single insert, update, or delete captured during a change timeline session.
/// All column values are stored as safe string representations; sensitive values
/// must be masked by the provider before being recorded.
/// </summary>
/// <param name="EventId">Unique identifier for this event, assigned by the capture service.</param>
/// <param name="SessionId">Identifier of the capture session that recorded this event.</param>
/// <param name="Timestamp">UTC timestamp when the change was captured.</param>
/// <param name="Operation">The type of data modification.</param>
/// <param name="DatabaseName">The name of the database where the change occurred.</param>
/// <param name="SchemaName">The schema that owns the affected table.</param>
/// <param name="TableName">The name of the affected table.</param>
/// <param name="PrimaryKeyColumns">Ordered list of primary key column names for the affected table.</param>
/// <param name="PrimaryKeyValues">Primary key column name → masked value mapping that identifies the affected row.</param>
/// <param name="Changes">Column name → before/after values for all changed columns. Empty for delete events when row data is unavailable.</param>
/// <param name="TraceId">Optional OpenTelemetry trace identifier correlated with this change, if available.</param>
/// <param name="TransactionId">Optional database transaction identifier, if available.</param>
public sealed record DataChangeEvent(
    string EventId,
    string SessionId,
    DateTimeOffset Timestamp,
    DataChangeOperation Operation,
    string DatabaseName,
    string SchemaName,
    string TableName,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyDictionary<string, string?> PrimaryKeyValues,
    IReadOnlyDictionary<string, ColumnChange> Changes,
    string? TraceId,
    string? TransactionId);
