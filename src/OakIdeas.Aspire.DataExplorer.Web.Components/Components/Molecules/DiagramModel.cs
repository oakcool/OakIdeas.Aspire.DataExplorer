namespace OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

/// <summary>
/// Top-level model passed to the JS diagram engine via JSON serialisation.
/// </summary>
public sealed record DiagramModel(
    IReadOnlyList<DiagramEntityNode> Entities,
    IReadOnlyList<DiagramRelationshipEdge> Relationships);

