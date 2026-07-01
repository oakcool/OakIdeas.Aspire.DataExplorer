namespace OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

/// <summary>
/// Top-level model passed to the JS diagram engine via JSON serialisation.
/// </summary>
public sealed record DiagramModel(
    IReadOnlyList<DiagramEntityNode> Entities,
    IReadOnlyList<DiagramRelationshipEdge> Relationships);

/// <summary>
/// Represents a single database entity (table or view) in the diagram.
/// </summary>
public sealed record DiagramEntityNode(
    string Id,
    string Name,
    string Schema,
    string EntityType,
    IReadOnlyList<DiagramColumnItem> Columns);

/// <summary>
/// Represents a single column within a <see cref="DiagramEntityNode"/>.
/// </summary>
public sealed record DiagramColumnItem(
    string Name,
    string DataType,
    bool IsPrimaryKey,
    bool IsForeignKey,
    bool IsNullable,
    bool IsIdentity);

/// <summary>
/// Represents a foreign-key relationship between two entities.
/// </summary>
public sealed record DiagramRelationshipEdge(
    string Id,
    string ConstraintName,
    string ParentEntityId,
    string ReferencedEntityId,
    IReadOnlyList<DiagramColumnMapping> ColumnMappings);

/// <summary>
/// Maps a parent column to the referenced column in a foreign-key relationship.
/// </summary>
public sealed record DiagramColumnMapping(
    string ParentColumn,
    string ReferencedColumn);
