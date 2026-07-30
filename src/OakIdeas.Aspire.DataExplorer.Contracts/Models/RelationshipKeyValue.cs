namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// A key-value pair representing a single column value used to look up related records.
/// </summary>
public sealed record RelationshipKeyValue(
    string ColumnName,
    string? ColumnValue);
