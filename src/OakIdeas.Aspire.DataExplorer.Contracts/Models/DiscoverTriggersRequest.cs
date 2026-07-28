using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverTriggersRequest(
    string? SchemaName = null,
    string? ParentObjectName = null);
