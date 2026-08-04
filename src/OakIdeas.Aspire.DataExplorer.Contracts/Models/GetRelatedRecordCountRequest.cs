namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to fetch a preview count of related records before loading the full result set.
/// </summary>
public sealed record GetRelatedRecordCountRequest(
    string SchemaName,
    string TableName,
    string ConstraintName,
    IReadOnlyList<RelationshipKeyValue> KeyValues);
