namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverResourcesResponse(
    IReadOnlyList<DiscoveredDatabaseResource> Resources);
