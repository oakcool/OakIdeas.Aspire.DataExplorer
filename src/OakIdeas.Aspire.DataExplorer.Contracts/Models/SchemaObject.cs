using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record SchemaObject : DatabaseObject
{
    public SchemaObject(
        string objectId,
        string objectName,
        string? description = null,
        IReadOnlyDictionary<string, object?>? providerMetadata = null,
        IReadOnlyList<DatabaseObjectRelationship>? relationships = null)
        : base(
            objectId,
            objectName,
            objectName,
            DatabaseObjectType.Schema,
            description,
            providerMetadata,
            relationships)
    {
    }
}
