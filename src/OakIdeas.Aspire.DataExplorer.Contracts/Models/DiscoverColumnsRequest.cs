using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverColumnsRequest(
    string? ObjectId = null,
    string? FullyQualifiedName = null,
    DatabaseObjectType ObjectType = DatabaseObjectType.Table);

