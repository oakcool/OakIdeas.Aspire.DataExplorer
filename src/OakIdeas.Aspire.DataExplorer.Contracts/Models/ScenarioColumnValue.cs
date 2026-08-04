namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Specifies the value for a single column within a scenario table operation.
/// </summary>
/// <param name="ColumnName">The name of the column.</param>
/// <param name="ValueKind">How the column value is determined.</param>
/// <param name="FixedValue">
/// The literal value to use when <see cref="ValueKind"/> is <see cref="ScenarioValueKind.Fixed"/>.
/// Stored as a string; the execution engine converts it to the target column type.
/// </param>
/// <param name="GeneratorName">
/// The name of the value generator to invoke when <see cref="ValueKind"/> is <see cref="ScenarioValueKind.Generated"/>.
/// Examples: <c>guid</c>, <c>utcnow</c>, <c>randomstring(8)</c>.
/// </param>
/// <param name="ReferenceAlias">
/// The alias of a preceding table operation whose generated primary key value should be used
/// when <see cref="ValueKind"/> is <see cref="ScenarioValueKind.Reference"/>.
/// </param>
/// <param name="ReferenceColumn">
/// The column name from the referenced operation's output to use. Defaults to the first primary key column when null.
/// </param>
public sealed record ScenarioColumnValue(
    string ColumnName,
    ScenarioValueKind ValueKind,
    string? FixedValue = null,
    string? GeneratorName = null,
    string? ReferenceAlias = null,
    string? ReferenceColumn = null);
