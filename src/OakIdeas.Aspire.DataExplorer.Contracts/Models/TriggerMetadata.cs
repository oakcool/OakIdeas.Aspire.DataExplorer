using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record TriggerMetadata(
    string TriggerName,
    string SchemaName,
    string ParentObjectName,
    TriggerParentObjectType ParentObjectType,
    TriggerType TriggerType,
    bool IsEnabled,
    bool HasDefinitionAvailable,
    string ObjectId,
    DateTimeOffset? CreatedAt,
    string? ParentSchemaName = null);
