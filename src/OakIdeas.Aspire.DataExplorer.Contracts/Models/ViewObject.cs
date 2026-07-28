using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ViewObject : SchemaBoundDatabaseObject
{
    public ViewObject(
        string objectId,
        string schemaName,
        string objectName,
        bool hasDefinitionAvailable = false,
        string? description = null,
        IReadOnlyDictionary<string, object?>? providerMetadata = null,
        IReadOnlyList<DatabaseObjectRelationship>? relationships = null)
        : base(
            objectId,
            schemaName,
            objectName,
            DatabaseObjectType.View,
            description,
            providerMetadata,
            relationships)
    {
        HasDefinitionAvailable = hasDefinitionAvailable;
    }

    public bool HasDefinitionAvailable { get; }
}
