namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record KeyMetadata(
    string Name,
    string Type,
    IReadOnlyList<string> Columns);

