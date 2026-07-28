using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

/// <summary>
/// Controls which data-editing context menu items are shown.
/// </summary>
/// <param name="InsertEnabled">Whether the INSERT statement item is visible.</param>
/// <param name="DeleteEnabled">Whether the DELETE statement and RESET items are visible.</param>
public sealed record DataEditingOptions(
    bool InsertEnabled = true,
    bool DeleteEnabled = true);

