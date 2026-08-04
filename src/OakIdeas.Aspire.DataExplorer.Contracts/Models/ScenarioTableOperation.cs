namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Represents a single table's insert operation within a test data scenario.
/// </summary>
/// <param name="SchemaName">The schema that owns the target table.</param>
/// <param name="TableName">The name of the target table.</param>
/// <param name="Alias">Optional short alias used when referencing generated keys from this operation in later operations.</param>
/// <param name="Columns">The column values to insert. Each entry specifies how a column's value is determined.</param>
public sealed record ScenarioTableOperation(
    string SchemaName,
    string TableName,
    string? Alias,
    IReadOnlyList<ScenarioColumnValue> Columns);
