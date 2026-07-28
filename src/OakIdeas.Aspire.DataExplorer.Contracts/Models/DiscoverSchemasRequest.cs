using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverSchemasRequest(
    bool IncludeSystemSchemas = false);
