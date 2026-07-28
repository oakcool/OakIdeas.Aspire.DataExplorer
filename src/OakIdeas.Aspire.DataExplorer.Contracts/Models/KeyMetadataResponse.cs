namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record KeyMetadataResponse(
    string Name,
    string Type,
    IReadOnlyList<string> Columns);

