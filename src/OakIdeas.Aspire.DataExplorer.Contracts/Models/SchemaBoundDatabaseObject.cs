using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public abstract record SchemaBoundDatabaseObject : DatabaseObject
{
    protected SchemaBoundDatabaseObject(
        string objectId,
        string schemaName,
        string objectName,
        DatabaseObjectType objectType,
        string? description = null,
        IReadOnlyDictionary<string, object?>? providerMetadata = null,
        IReadOnlyList<DatabaseObjectRelationship>? relationships = null)
        : base(
            objectId,
            objectName,
            BuildFullyQualifiedName(schemaName, objectName),
            objectType,
            description,
            providerMetadata,
            relationships)
    {
        SchemaName = EnsureRequired(schemaName, nameof(schemaName));
    }

    public string SchemaName { get; }

    private static string BuildFullyQualifiedName(string schemaName, string objectName)
        => $"{EnsureRequired(schemaName, nameof(schemaName))}.{EnsureRequired(objectName, nameof(objectName))}";

    private static string EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}

