using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

/// <summary>
/// Represents a single item in the Object Explorer context menu.
/// </summary>
public sealed class ExplorerContextMenuItem
{
    /// <summary>Gets the unique identifier for this menu item.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the display label for this menu item.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Gets the optional icon for this menu item.</summary>
    public HeroIconKind? Icon { get; init; }

    /// <summary>Gets whether this item is enabled and can be clicked.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Gets whether this item is a visual separator.</summary>
    public bool IsSeparator { get; init; }

    /// <summary>Gets the async action to execute when this item is clicked.</summary>
    public Func<Task>? Action { get; init; }

    /// <summary>Gets the optional nested child items.</summary>
    public IReadOnlyList<ExplorerContextMenuItem>? Children { get; init; }

    /// <summary>Creates a separator item.</summary>
    public static ExplorerContextMenuItem Separator => new() { IsSeparator = true, Id = "separator" };
}
