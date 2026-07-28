using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverIndexesRequest(
    string? SchemaName = null,
    string? TableName = null);

