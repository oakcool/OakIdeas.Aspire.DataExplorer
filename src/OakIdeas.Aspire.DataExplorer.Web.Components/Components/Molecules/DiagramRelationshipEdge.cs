namespace OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

/// <summary>
/// Represents a foreign-key relationship between two entities.
/// </summary>
public sealed record DiagramRelationshipEdge(
    string Id,
    string ConstraintName,
    string ParentEntityId,
    string ReferencedEntityId,
    IReadOnlyList<DiagramColumnMapping> ColumnMappings);
