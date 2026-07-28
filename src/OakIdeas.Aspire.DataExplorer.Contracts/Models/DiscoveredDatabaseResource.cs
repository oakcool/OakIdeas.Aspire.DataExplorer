namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoveredDatabaseResource(
    string ResourceId,
    string ResourceName,
    string DatabaseName,
    DatabaseProviderType ProviderType,
    ConnectionMetadata ConnectionMetadata,
    bool IsAvailable,
    DateTimeOffset DiscoveredAt);
