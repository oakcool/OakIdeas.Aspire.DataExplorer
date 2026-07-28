using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverIndexesResponse(
    IReadOnlyList<IndexMetadata> Indexes);
