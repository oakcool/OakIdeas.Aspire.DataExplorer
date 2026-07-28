using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverStoredProceduresRequest(
    string? SchemaName = null,
    bool IncludeSystemProcedures = false);
