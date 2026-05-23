using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class QueryPanelRenderingTests : TestContext
{
    public QueryPanelRenderingTests()
    {
        // Allow JS interop calls to succeed silently (no real browser in tests)
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

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
    public void Suggestions_NoDropdownPanel_GhostTextShownInHighlightLayer()
    {
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SEL")
            .Add(p => p.Suggestions, ["SELECT", "FROM", "WHERE"]));

        component.Find("textarea").Input("SEL");

        // No suggestion button panel — ghost text appears in the highlight <pre>
        component.Markup.Should().NotContain("query-panel__suggestion");
        component.Markup.Should().NotContain("query-panel__autocomplete");
        component.Markup.Should().Contain("query-panel__ghost");
        // Ghost text = remainder of "SELECT" after "SEL"
        component.Markup.Should().Contain("ECT");
    }

    [Fact]
    public async Task HandleTab_WithActiveSuggestion_CompletesSql()
    {
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Suggestions, ["SELECT", "FROM", "WHERE"]));

        component.Find("textarea").Input("SEL");

        // HandleTab is JSInvokable — called by JS when Tab is pressed
        await component.InvokeAsync(() => component.Instance.HandleTab());

        component.Instance.Sql.Should().Be("SELECT ");
    }

    [Fact]
    public async Task HandleTab_WithNoSuggestion_DoesNothing()
    {
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Suggestions, ["SELECT", "FROM", "WHERE"]));

        component.Find("textarea").Input("XYZ_NO_MATCH");

        await component.InvokeAsync(() => component.Instance.HandleTab());

        component.Instance.Sql.Should().Be("XYZ_NO_MATCH");
    }

    [Fact]
    public void SyntaxHighlighting_KeywordsWrappedInSpan()
    {
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SELECT * FROM users"));

        // Trigger re-render with the SQL set
        component.Find("textarea").Input("SELECT * FROM users");

        component.Markup.Should().Contain("sql-keyword");
        component.Markup.Should().Contain("SELECT");
        component.Markup.Should().Contain("FROM");
    }

    [Fact]
    public async Task Execute_WithSelectedText_ExecutesOnlySelection()
    {
        // Configure the module's getSelectedText to return a selection
        var module = JSInterop.SetupModule(
            "./_content/OakIdeas.Aspire.DataExplorer.Web.Components/Components/Molecules/QueryPanel.razor.js");
        module.Setup<string?>("getSelectedText", _ => true).SetResult("WHERE Id = 1");

        string? executedSql = null;
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SELECT * FROM Users WHERE Id = 1")
            .Add(p => p.OnExecute, EventCallback.Factory.Create<string>(this, sql => executedSql = sql)));

        component.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        await component.InvokeAsync(() => Task.CompletedTask);

        executedSql.Should().Be("WHERE Id = 1");
    }

    [Fact]
    public void Execute_WithNoSelection_ExecutesFullSql()
    {
        // Loose mode — getSelectedText returns null/empty → full SQL is used
        string? executedSql = null;
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Sql, "SELECT 1")
            .Add(p => p.OnExecute, EventCallback.Factory.Create<string>(this, sql => executedSql = sql)));

        component.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        executedSql.Should().Be("SELECT 1");
    }

    [Fact]
    public void PartialToken_MatchesContainsSuggestion()
    {
        var component = RenderComponent<QueryPanel>(parameters => parameters
            .Add(p => p.Suggestions, ["FROM", "TRANSFORM"]));

        // "OM" matches both FROM (contains) and TRANSFORM (contains)
        component.Find("textarea").Input("OM");

        // Ghost text should show something (from a matched suggestion)
        component.Markup.Should().Contain("query-panel__ghost");
    }
}

