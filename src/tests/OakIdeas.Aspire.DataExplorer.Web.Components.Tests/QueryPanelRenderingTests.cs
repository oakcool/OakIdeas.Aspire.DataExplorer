using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class QueryPanelRenderingTests : TestContext
{
    [Fact]
    public void RendersDisconnectedBadgeWhenConnectionMissing()
    {
        var component = RenderComponent<QueryPanel>();

        component.Markup.Should().Contain("No connection");
    }

    [Fact]
    public void CtrlEnterInvokesExecuteCallback()
    {
        string? executedSql = null;
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SELECT 1")
            .Add(p => p.OnExecute, EventCallback.Factory.Create<string>(this, sql => executedSql = sql)));

        component.Find("textarea").KeyDown(new KeyboardEventArgs
        {
            Key = "Enter",
            CtrlKey = true,
        });

        executedSql.Should().Be("SELECT 1");
    }

    [Fact]
    public void CancelButtonInvokesCancelCallback()
    {
        var cancelled = false;
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.IsExecuting, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelled = true)));

        component.Find("button[title='Cancel running query']").Click();

        cancelled.Should().BeTrue();
    }
}
