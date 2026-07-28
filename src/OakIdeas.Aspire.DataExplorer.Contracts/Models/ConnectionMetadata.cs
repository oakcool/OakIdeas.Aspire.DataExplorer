namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ConnectionMetadata(
    IReadOnlyDictionary<string, string?> Properties);
