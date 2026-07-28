namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record TableMetadata(
    string Schema,
    string Name,
    IReadOnlyList<ColumnMetadata> Columns,
    IReadOnlyList<KeyMetadata> Keys,
    IReadOnlyList<RelationshipMetadata> Relationships);

