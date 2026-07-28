namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record TableMetadataResponse(
    string Schema,
    string Name,
    IReadOnlyList<ColumnMetadataResponse> Columns,
    IReadOnlyList<KeyMetadataResponse> Keys);

