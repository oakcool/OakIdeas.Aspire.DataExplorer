using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ProcedureObject : SchemaBoundDatabaseObject
{
    public ProcedureObject(
        string objectId,
        string schemaName,
        string objectName,
        string? description = null,
        IReadOnlyDictionary<string, object?>? providerMetadata = null,
        IReadOnlyList<DatabaseObjectRelationship>? relationships = null)
        : base(
            objectId,
            schemaName,
            objectName,
            DatabaseObjectType.Procedure,
            description,
            providerMetadata,
            relationships)
    {
    }
}
