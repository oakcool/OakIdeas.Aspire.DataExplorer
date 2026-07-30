namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Describes the directionality of a navigable table relationship.
/// </summary>
public enum RelationshipKind
{
    /// <summary>
    /// The current table is the child (foreign key holder). Navigation leads to the referenced (parent) table.
    /// </summary>
    Parent = 1,

    /// <summary>
    /// The current table is the parent (primary key holder). Navigation leads to referencing (child) tables.
    /// </summary>
    Child = 2,

    /// <summary>
    /// A many-to-many relationship resolved through a junction table.
    /// Navigation leads to the other side of the junction.
    /// </summary>
    ManyToMany = 3,
}
