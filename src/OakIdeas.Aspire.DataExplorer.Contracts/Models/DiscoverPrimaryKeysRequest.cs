using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverPrimaryKeysRequest(
    string? SchemaName = null,
    string? TableName = null);
