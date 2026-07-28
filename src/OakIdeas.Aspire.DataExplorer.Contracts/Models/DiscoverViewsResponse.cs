using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverViewsResponse(
    IReadOnlyList<ViewObject> Views);
