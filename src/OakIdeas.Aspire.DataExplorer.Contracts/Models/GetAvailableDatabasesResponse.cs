namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record GetAvailableDatabasesResponse(
    IReadOnlyList<DiscoveredDatabaseResource> Resources,
    DataExplorerError? Error = null);

