using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SchemaObject), "schema")]
[JsonDerivedType(typeof(TableObject), "table")]
[JsonDerivedType(typeof(ViewObject), "view")]
[JsonDerivedType(typeof(ProcedureObject), "procedure")]
[JsonDerivedType(typeof(FunctionObject), "function")]
[JsonDerivedType(typeof(TriggerObject), "trigger")]
public abstract record DatabaseObject
{
    protected DatabaseObject(
        string objectId,
        string objectName,
        string fullyQualifiedName,
        DatabaseObjectType objectType,
        string? description = null,
        IReadOnlyDictionary<string, object?>? providerMetadata = null,
        IReadOnlyList<DatabaseObjectRelationship>? relationships = null)
    {
        ObjectId = EnsureRequired(objectId, nameof(objectId));
        ObjectName = EnsureRequired(objectName, nameof(objectName));
        FullyQualifiedName = EnsureRequired(fullyQualifiedName, nameof(fullyQualifiedName));
        ObjectType = objectType;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ProviderMetadata = providerMetadata ?? new Dictionary<string, object?>();
        Relationships = relationships ?? Array.Empty<DatabaseObjectRelationship>();
    }

    public string ObjectId { get; }
    public string ObjectName { get; }
    public string FullyQualifiedName { get; }
    public string? Description { get; }
    public DatabaseObjectType ObjectType { get; }
    public IReadOnlyDictionary<string, object?> ProviderMetadata { get; }
    public IReadOnlyList<DatabaseObjectRelationship> Relationships { get; }

    private static string EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}
