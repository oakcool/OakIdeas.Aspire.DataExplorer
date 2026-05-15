namespace OakIdeas.Aspire.DataExplorer.Core.Models;

internal sealed record DiscoveredDatabaseResourceDescriptor(
    string? ResourceId,
    string? ResourceName,
    string? DatabaseName,
    string? ProviderHint,
    IReadOnlyDictionary<string, string?>? ConnectionMetadata,
    bool IsAvailable);
