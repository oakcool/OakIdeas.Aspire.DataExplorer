using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverForeignKeysRequest(
    string? ParentSchemaName = null,
    string? ParentTableName = null);
