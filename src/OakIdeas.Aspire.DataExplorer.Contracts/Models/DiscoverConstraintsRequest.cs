using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverConstraintsRequest(
    string? SchemaName = null,
    string? TableName = null);

