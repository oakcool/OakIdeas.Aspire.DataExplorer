namespace OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

/// <summary>
/// Represents a single database entity (table or view) in the diagram.
/// </summary>
public sealed record DiagramEntityNode(
    string Id,
    string Name,
    string Schema,
    string EntityType,
    IReadOnlyList<DiagramColumnItem> Columns);

