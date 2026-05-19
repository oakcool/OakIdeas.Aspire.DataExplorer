using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class QueryPanelRenderingTests : TestContext
{
    [Fact]
    public void CtrlEnter_InvokesExecuteCallback()
    {
        string? executedSql = null;
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SELECT 1")
            .Add(p => p.OnExecute, EventCallback.Factory.Create<string>(this, sql => executedSql = sql)));

        component.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        executedSql.Should().Be("SELECT 1");
    }

    [Fact]
    public void CancelButton_InvokesCancelCallback()
    {
        var canceled = false;
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SELECT 1")
            .Add(p => p.IsExecuting, true)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => canceled = true)));

        component.Find("button[title='Cancel current query']").Click();

        canceled.Should().BeTrue();
    }

    [Fact]
    public void Suggestions_RenderAndCanBeApplied()
    {
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SEL")
            .Add(p => p.Suggestions, ["SELECT", "FROM", "WHERE"]));

        component.Find("textarea").Input("SEL");

        component.Markup.Should().Contain("SELECT");
        component.Find("button.query-panel__suggestion").Click();
        component.Instance.Sql.Should().Be("SELECT ");
    }
}
