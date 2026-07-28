namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record RelationshipMetadata(
    string Name,
    string FromSchema,
    string FromTable,
    string ToSchema,
    string ToTable,
    IReadOnlyList<string> FromColumns,
    IReadOnlyList<string> ToColumns);
