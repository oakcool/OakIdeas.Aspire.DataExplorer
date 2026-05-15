namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record DiscoveredDatabaseResourceDescriptor(
    string? ResourceId,
    string? ResourceName,
    string? DatabaseName,
    string? ProviderHint,
    IReadOnlyDictionary<string, string?>? ConnectionMetadata,
    bool IsAvailable);
