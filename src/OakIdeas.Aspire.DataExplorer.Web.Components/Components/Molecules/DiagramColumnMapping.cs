namespace OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

/// <summary>
/// Maps a parent column to the referenced column in a foreign-key relationship.
/// </summary>
public sealed record DiagramColumnMapping(
    string ParentColumn,
    string ReferencedColumn);
