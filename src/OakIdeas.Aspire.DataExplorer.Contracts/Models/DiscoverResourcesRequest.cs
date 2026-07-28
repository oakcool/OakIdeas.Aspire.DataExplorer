namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverResourcesRequest(
    bool? IncludeUnavailableResources = null);
