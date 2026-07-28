using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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

