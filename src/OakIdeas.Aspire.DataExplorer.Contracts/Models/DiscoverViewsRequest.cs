using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverViewsRequest(
    string? SchemaName = null,
    bool IncludeSystemViews = false);

