using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;
using OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ExplorerContextMenuBuilderTests
{
    [Fact]
    public void Build_ForTable_ContainsViewAction()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "view");
    }

    [Fact]
    public void Build_ForTable_ContainsSelectTop1000()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "select-top-1000");
    }

    [Fact]
    public void Build_ForTable_ContainsInsertStatement()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "insert-statement");
    }

    [Fact]
    public void Build_ForTable_ContainsDeleteStatement()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "delete-statement");
    }

    [Fact]
    public void Build_ForTable_ContainsResetStatement()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "reset-statement");
    }

    [Fact]
    public void Build_ForTable_ContainsScriptDefinition()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "script-definition");
    }

    [Fact]
    public void Build_ForView_ContainsOnlyViewAndScriptDefinition()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.View);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask)
            .Where(item => !item.IsSeparator)
            .ToList();

        items.Should().Contain(item => item.Id == "view");
        items.Should().Contain(item => item.Id == "script-definition");
        items.Should().NotContain(item => item.Id == "select-top-1000");
        items.Should().NotContain(item => item.Id == "insert-statement");
    }

    [Fact]
    public void Build_ForProcedure_ContainsExecuteProcedure()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Procedure);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "execute-procedure");
    }

    [Fact]
    public void Build_ForProcedure_ContainsScriptDefinition()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Procedure);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().Contain(item => item.Id == "script-definition");
    }

    [Fact]
    public void Build_ForProcedure_DoesNotContainTableActions()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Procedure);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask);

        items.Should().NotContain(item => item.Id == "select-top-1000");
        items.Should().NotContain(item => item.Id == "insert-statement");
        items.Should().NotContain(item => item.Id == "delete-statement");
    }

    [Fact]
    public void Build_ForFunction_ContainsViewAndScriptDefinition()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Function);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask)
            .Where(item => !item.IsSeparator)
            .ToList();

        items.Should().Contain(item => item.Id == "view");
        items.Should().Contain(item => item.Id == "script-definition");
    }

    [Fact]
    public void Build_ForTrigger_ContainsViewAndScriptDefinition()
    {
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Trigger);
        var items = ExplorerContextMenuBuilder.Build(selection, _ => Task.CompletedTask)
            .Where(item => !item.IsSeparator)
            .ToList();

        items.Should().Contain(item => item.Id == "view");
        items.Should().Contain(item => item.Id == "script-definition");
    }

    [Fact]
    public async Task Build_ViewAction_RaisesContextActionWithViewId()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var viewItem = items.Single(item => item.Id == "view");
        await viewItem.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.ActionId.Should().Be("view");
        raisedAction.Sql.Should().BeNull();
        raisedAction.AutoExecute.Should().BeFalse();
    }

    [Fact]
    public async Task Build_SelectTop1000Action_RaisesQueryActionWithAutoExecute()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var top1000Item = items.Single(item => item.Id == "select-top-1000");
        await top1000Item.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.AutoExecute.Should().BeTrue();
        raisedAction.Sql.Should().Contain("SELECT TOP 1000");
        raisedAction.Sql.Should().Contain("[dbo].[Users]");
    }

    [Fact]
    public async Task Build_InsertStatementAction_DoesNotAutoExecute()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var insertItem = items.Single(item => item.Id == "insert-statement");
        await insertItem.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.AutoExecute.Should().BeFalse();
        raisedAction.Sql.Should().Contain("INSERT INTO");
        raisedAction.Sql.Should().Contain("[dbo].[Users]");
    }

    [Fact]
    public async Task Build_DeleteStatementAction_UseSafeDefault()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var deleteItem = items.Single(item => item.Id == "delete-statement");
        await deleteItem.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.AutoExecute.Should().BeFalse();
        raisedAction.Sql.Should().Contain("DELETE FROM");
        raisedAction.Sql.Should().Contain("WHERE 1 = 0");
    }

    [Fact]
    public async Task Build_ResetStatementAction_GeneratesTruncate()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Table);
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var resetItem = items.Single(item => item.Id == "reset-statement");
        await resetItem.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.AutoExecute.Should().BeFalse();
        raisedAction.Sql.Should().Contain("TRUNCATE TABLE");
        raisedAction.Sql.Should().Contain("[dbo].[Users]");
    }

    [Fact]
    public async Task Build_ExecuteProcedureAction_GeneratesExecStatement()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.Procedure, "SyncUsers");
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var execItem = items.Single(item => item.Id == "execute-procedure");
        await execItem.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.AutoExecute.Should().BeFalse();
        raisedAction.Sql.Should().Contain("EXEC");
        raisedAction.Sql.Should().Contain("[dbo].[SyncUsers]");
    }

    [Fact]
    public async Task Build_ScriptDefinitionAction_GeneratesSpHelptext()
    {
        ExplorerContextAction? raisedAction = null;
        var selection = CreateSelection(ObjectExplorer.ObjectKind.View, "ActiveUsers");
        var items = ExplorerContextMenuBuilder.Build(selection, action =>
        {
            raisedAction = action;
            return Task.CompletedTask;
        });

        var scriptItem = items.Single(item => item.Id == "script-definition");
        await scriptItem.Action!();

        raisedAction.Should().NotBeNull();
        raisedAction!.AutoExecute.Should().BeFalse();
        raisedAction.Sql.Should().Contain("sp_helptext");
        raisedAction.Sql.Should().Contain("dbo.ActiveUsers");
    }

    private static ObjectExplorer.ObjectSelection CreateSelection(
        ObjectExplorer.ObjectKind kind,
        string objectName = "Users")
        => new(
            ConnectionName: "sql-main",
            DatabaseName: "applicationdb",
            SchemaName: "dbo",
            ObjectId: $"dbo.{objectName}",
            ObjectName: objectName,
            ObjectKind: kind);
}
