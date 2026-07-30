namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Maps a source column to the corresponding column in the related table for a <see cref="TableRelationship"/>.
/// </summary>
public sealed record RelationshipColumnMapping(
    string SourceColumnName,
    string RelatedColumnName);
