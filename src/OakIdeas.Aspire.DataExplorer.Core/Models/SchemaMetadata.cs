namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record SchemaMetadata(
    string Name,
    IReadOnlyList<TableMetadata> Tables,
    IReadOnlyList<ViewMetadata> Views);
