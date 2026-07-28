using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverTablesRequest(
    string? SchemaName = null,
    bool IncludeSystemTables = false);

