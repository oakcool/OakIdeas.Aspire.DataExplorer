namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to navigate to related records across a specific relationship, optionally paginated.
/// </summary>
public sealed record NavigateRelatedRecordsRequest(
    string SchemaName,
    string TableName,
    string ConstraintName,
    IReadOnlyList<RelationshipKeyValue> KeyValues,
    int PageSize = 100,
    int PageNumber = 1);
