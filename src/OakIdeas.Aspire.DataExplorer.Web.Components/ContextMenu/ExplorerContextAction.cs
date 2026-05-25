using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

/// <summary>
/// Represents a context menu action raised from the Object Explorer.
/// </summary>
/// <param name="ActionId">The identifier of the action that was invoked.</param>
/// <param name="Source">The object on which the action was invoked.</param>
/// <param name="Sql">The generated SQL, if applicable.</param>
/// <param name="AutoExecute">Whether the SQL should be automatically executed.</param>
public record ExplorerContextAction(
    string ActionId,
    ObjectExplorer.ObjectSelection Source,
    string? Sql = null,
    bool AutoExecute = false);
