using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum DatabaseObjectType
{
    Unknown = 0,
    Schema = 1,
    Table = 2,
    View = 3,
    Procedure = 4,
    Function = 5,
    Trigger = 6,
}

public sealed record DatabaseObjectRelationship(
    string RelationshipName,
    string RelationshipType,
    string TargetObjectId,
    string? Description = null)
{
    public string RelationshipName { get; } = EnsureRequired(RelationshipName, nameof(RelationshipName));
    public string RelationshipType { get; } = EnsureRequired(RelationshipType, nameof(RelationshipType));
    public string TargetObjectId { get; } = EnsureRequired(TargetObjectId, nameof(TargetObjectId));

    private static string EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}

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

public sealed record TableObject : SchemaBoundDatabaseObject
{
    public TableObject(
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
            DatabaseObjectType.Table,
            description,
            providerMetadata,
            relationships)
    {
    }
}

public sealed record ViewObject : SchemaBoundDatabaseObject
{
    public ViewObject(
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
            DatabaseObjectType.View,
            description,
            providerMetadata,
            relationships)
    {
    }
}

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

public sealed record FunctionObject : SchemaBoundDatabaseObject
{
    public FunctionObject(
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
            DatabaseObjectType.Function,
            description,
            providerMetadata,
            relationships)
    {
    }
}

public sealed record TriggerObject : SchemaBoundDatabaseObject
{
    public TriggerObject(
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
            DatabaseObjectType.Trigger,
            description,
            providerMetadata,
            relationships)
    {
    }
}

public sealed record DatabaseMetadataRoot
{
    public DatabaseMetadataRoot(
        string databaseName,
        DatabaseProviderType providerType,
        string resourceId,
        DateTimeOffset metadataCollectionTime,
        IReadOnlyDictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>? objects = null)
    {
        DatabaseName = EnsureRequired(databaseName, nameof(databaseName));
        ProviderType = providerType;
        ResourceId = EnsureRequired(resourceId, nameof(resourceId));
        MetadataCollectionTime = metadataCollectionTime;
        Objects = NormalizeObjects(objects);
    }

    public string DatabaseName { get; }
    public DatabaseProviderType ProviderType { get; }
    public string ResourceId { get; }
    public DateTimeOffset MetadataCollectionTime { get; }
    public IReadOnlyDictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>> Objects { get; }

    private static IReadOnlyDictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>> NormalizeObjects(
        IReadOnlyDictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>? objects)
    {
        if (objects is null || objects.Count == 0)
        {
            return new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>();
        }

        var normalized = new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>();

        foreach (var (objectType, entries) in objects)
        {
            var typedEntries = new Dictionary<string, DatabaseObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in entries)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException("Object dictionary keys must be populated.", nameof(objects));
                }

                typedEntries[key.Trim()] = value
                    ?? throw new ArgumentException("Object dictionary values cannot be null.", nameof(objects));
            }

            normalized[objectType] = typedEntries;
        }

        return normalized;
    }

    private static string EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record DiscoverDatabaseMetadataRequest(
    string ResourceId,
    string DatabaseName);

public sealed record DiscoverDatabaseMetadataResponse(
    DatabaseMetadataRoot Metadata);
