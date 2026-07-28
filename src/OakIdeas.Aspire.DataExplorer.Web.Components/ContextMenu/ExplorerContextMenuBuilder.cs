using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

/// <summary>
/// Builds context menu items for a given Object Explorer selection.
/// This builder is the extensibility point for adding new context menu actions.
/// </summary>
public static class ExplorerContextMenuBuilder
{
    /// <summary>
    /// Builds context menu items appropriate for the given object selection.
    /// </summary>
    /// <param name="selection">The selected explorer object.</param>
    /// <param name="onAction">Callback invoked when a menu action is triggered.</param>
    /// <param name="dataEditing">Controls which data-editing items are visible. Defaults to all enabled.</param>
    /// <returns>The menu items to display.</returns>
    public static IReadOnlyList<ExplorerContextMenuItem> Build(
        ObjectExplorer.ObjectSelection selection,
        Func<ExplorerContextAction, Task> onAction,
        DataEditingOptions? dataEditing = null)
    {
        dataEditing ??= new DataEditingOptions();
        var items = new List<ExplorerContextMenuItem>
        {
            new()
            {
                Id = "view",
                Label = "View",
                Icon = HeroIconKind.Eye,
                Action = () => onAction(new ExplorerContextAction("view", selection))
            }
        };

        switch (selection.ObjectKind)
        {
            case ObjectExplorer.ObjectKind.Table:
                AddTableItems(items, selection, onAction, dataEditing);
                break;

            case ObjectExplorer.ObjectKind.View:
            case ObjectExplorer.ObjectKind.Function:
            case ObjectExplorer.ObjectKind.Trigger:
                AddScriptDefinitionItem(items, selection, onAction);
                break;

            case ObjectExplorer.ObjectKind.Procedure:
                AddProcedureItems(items, selection, onAction);
                break;
        }

        return items;
    }

    private static void AddTableItems(
        List<ExplorerContextMenuItem> items,
        ObjectExplorer.ObjectSelection selection,
        Func<ExplorerContextAction, Task> onAction,
        DataEditingOptions dataEditing)
    {
        items.Add(ExplorerContextMenuItem.Separator);

        items.Add(new ExplorerContextMenuItem
        {
            Id = "select-top-1000",
            Label = "Select TOP 1000 Rows",
            Icon = HeroIconKind.TableCells,
            Action = () => onAction(new ExplorerContextAction(
                "select-top-1000",
                selection,
                Sql: ExplorerQueryTemplates.SelectTop1000(selection.SchemaName, selection.ObjectName),
                AutoExecute: true))
        });

        if (dataEditing.InsertEnabled)
        {
            items.Add(new ExplorerContextMenuItem
            {
                Id = "insert-statement",
                Label = "INSERT Statement",
                Icon = HeroIconKind.Plus,
                Action = () => onAction(new ExplorerContextAction(
                    "insert-statement",
                    selection,
                    Sql: ExplorerQueryTemplates.InsertStatement(selection.SchemaName, selection.ObjectName),
                    AutoExecute: false))
            });
        }

        if (dataEditing.DeleteEnabled)
        {
            items.Add(new ExplorerContextMenuItem
            {
                Id = "delete-statement",
                Label = "DELETE Statement",
                Icon = HeroIconKind.Trash,
                Action = () => onAction(new ExplorerContextAction(
                    "delete-statement",
                    selection,
                    Sql: ExplorerQueryTemplates.DeleteStatement(selection.SchemaName, selection.ObjectName),
                    AutoExecute: false))
            });

            items.Add(new ExplorerContextMenuItem
            {
                Id = "reset-statement",
                Label = "RESET / Truncate",
                Icon = HeroIconKind.ArrowPath,
                Action = () => onAction(new ExplorerContextAction(
                    "reset-statement",
                    selection,
                    Sql: ExplorerQueryTemplates.TruncateStatement(selection.SchemaName, selection.ObjectName),
                    AutoExecute: false))
            });
        }

        items.Add(ExplorerContextMenuItem.Separator);

        AddScriptDefinitionItem(items, selection, onAction);
    }

    private static void AddProcedureItems(
        List<ExplorerContextMenuItem> items,
        ObjectExplorer.ObjectSelection selection,
        Func<ExplorerContextAction, Task> onAction)
    {
        items.Add(ExplorerContextMenuItem.Separator);

        items.Add(new ExplorerContextMenuItem
        {
            Id = "execute-procedure",
            Label = "Execute Procedure",
            Icon = HeroIconKind.Play,
            Action = () => onAction(new ExplorerContextAction(
                "execute-procedure",
                selection,
                Sql: ExplorerQueryTemplates.ExecuteProcedure(selection.SchemaName, selection.ObjectName),
                AutoExecute: false))
        });

        items.Add(ExplorerContextMenuItem.Separator);

        AddScriptDefinitionItem(items, selection, onAction);
    }

    private static void AddScriptDefinitionItem(
        List<ExplorerContextMenuItem> items,
        ObjectExplorer.ObjectSelection selection,
        Func<ExplorerContextAction, Task> onAction)
    {
        items.Add(new ExplorerContextMenuItem
        {
            Id = "script-definition",
            Label = "Script Definition",
            Icon = HeroIconKind.CodeBracket,
            Action = () => onAction(new ExplorerContextAction(
                "script-definition",
                selection,
                Sql: ExplorerQueryTemplates.ScriptDefinition(selection.SchemaName, selection.ObjectName),
                AutoExecute: false))
        });
    }
}

