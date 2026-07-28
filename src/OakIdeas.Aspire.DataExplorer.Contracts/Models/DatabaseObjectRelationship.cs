using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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
