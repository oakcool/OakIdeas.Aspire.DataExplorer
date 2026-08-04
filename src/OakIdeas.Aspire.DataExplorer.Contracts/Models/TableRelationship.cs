namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Describes a navigable relationship from a source table to a related table.
/// Used by the Relationship-Aware Data Navigator to present parent, child,
/// and many-to-many navigation links.
/// </summary>
public sealed record TableRelationship
{
    /// <summary>The foreign key or relationship constraint name.</summary>
    public required string ConstraintName { get; init; }

    /// <summary>The kind of relationship from the perspective of the source table.</summary>
    public required RelationshipKind Kind { get; init; }

    /// <summary>Schema name of the related (target) table.</summary>
    public required string RelatedSchemaName { get; init; }

    /// <summary>Name of the related (target) table.</summary>
    public required string RelatedTableName { get; init; }

    /// <summary>
    /// Column mappings between the source and related table.
    /// Each entry maps a source column name to a related column name.
    /// </summary>
    public required IReadOnlyList<RelationshipColumnMapping> ColumnMappings { get; init; }

    /// <summary>
    /// For <see cref="RelationshipKind.ManyToMany"/> relationships, the schema of the junction table.
    /// <see langword="null"/> for direct parent/child relationships.
    /// </summary>
    public string? JunctionSchemaName { get; init; }

    /// <summary>
    /// For <see cref="RelationshipKind.ManyToMany"/> relationships, the name of the junction table.
    /// <see langword="null"/> for direct parent/child relationships.
    /// </summary>
    public string? JunctionTableName { get; init; }

    /// <summary>
    /// Indicates whether this relationship enforces referential integrity (i.e., the constraint is enabled).
    /// </summary>
    public bool IsEnforced { get; init; } = true;

    /// <summary>
    /// A human-readable label for display in the navigator UI.
    /// Defaults to <see cref="ConstraintName"/> when not explicitly set.
    /// </summary>
    public string DisplayLabel => $"{RelatedSchemaName}.{RelatedTableName} ({ConstraintName})";
}
