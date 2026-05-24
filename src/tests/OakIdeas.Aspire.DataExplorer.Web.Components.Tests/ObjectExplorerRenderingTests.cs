using Bunit;
using Microsoft.AspNetCore.Components;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ObjectExplorerRenderingTests : TestContext
{
    [Fact]
    public void RendersLoadingState()
    {
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.IsLoading, true));

        component.Markup.Should().Contain("Loading metadata objects");
    }

    [Fact]
    public void RendersEmptyState()
    {
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Connections, []));

        component.Markup.Should().Contain("No database objects were discovered.");
    }

    [Fact]
    public void RendersErrorState()
    {
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.ErrorMessage, "Unable to load metadata."));

        component.Markup.Should().Contain("Unable to load metadata.");
    }

    [Fact]
    public void RendersDiagnosticErrorDetails()
    {
        var error = new DataExplorerError(
            ErrorCategory.ConnectionFailed,
            "The selected database is currently unavailable.",
            "Confirm the database is running and try again.",
            "load-metadata",
            "applicationdb",
            DateTimeOffset.UtcNow,
            "sql-unavailable");

        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.ErrorMessage, error.Message)
            .Add(p => p.Error, error));

        component.Markup.Should().Contain("Confirm the database is running and try again.");
        component.Markup.Should().Contain("Diagnostic details");
        component.Markup.Should().Contain("sql-unavailable");
    }

    [Fact]
    public void RendersDatabaseGroupsAndObjects()
    {
        var connections = CreateConnections();
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.Connections, connections));

        component.Markup.Should().Contain("Tables");
        component.Markup.Should().Contain("Views");
        component.Markup.Should().Contain("Programmability");
        component.Markup.Should().Contain("Stored Procedures");
        component.Markup.Should().Contain("Functions");
        component.Markup.Should().Contain("Triggers");
        component.Markup.Should().Contain("Security");
        component.Markup.Should().Contain("Schemas");
        component.Markup.Should().Contain("dbo");
        component.Markup.Should().Contain("Users");
        component.Markup.Should().NotContain("dbo.Users");
        component.Markup.Should().Contain("ActiveUsers");
        component.Markup.Should().Contain("SyncUsers");
        component.Markup.Should().Contain("FormatName");
        component.Markup.Should().Contain("UsersAudit");
    }

    [Fact]
    public void RefreshButtonInvokesCallback()
    {
        var refreshCalled = false;
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.Connections, CreateConnections())
            .Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => refreshCalled = true)));

        component.Find("button[title='Refresh']").Click();

        refreshCalled.Should().BeTrue();
    }

    [Fact]
    public void ObjectClickInvokesSelectionCallback()
    {
        ObjectExplorer.ObjectSelection? selected = null;
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.Connections, CreateConnections())
            .Add(p => p.OnObjectSelect, EventCallback.Factory.Create<ObjectExplorer.ObjectSelection>(this, value => selected = value)));

        component.FindAll(".tree-node")
            .First(node => node.TextContent.Contains("Users"))
            .Click();

        selected.Should().NotBeNull();
        selected!.SchemaName.Should().Be("dbo");
        selected.ObjectName.Should().Be("Users");
        selected.ObjectKind.Should().Be(ObjectExplorer.ObjectKind.Table);
    }

    private static IReadOnlyList<ObjectExplorer.ConnectionNode> CreateConnections()
        =>
        [
            new ObjectExplorer.ConnectionNode("sql-main",
            [
                new ObjectExplorer.DatabaseNode("applicationdb",
                    [new ObjectExplorer.ObjectNodeModel("dbo.Users", "Users", "sql-main", "applicationdb", "dbo", "Users", ObjectExplorer.ObjectKind.Table)],
                    [new ObjectExplorer.ObjectNodeModel("dbo.ActiveUsers", "ActiveUsers", "sql-main", "applicationdb", "dbo", "ActiveUsers", ObjectExplorer.ObjectKind.View)],
                    [new ObjectExplorer.ObjectNodeModel("dbo.SyncUsers", "SyncUsers", "sql-main", "applicationdb", "dbo", "SyncUsers", ObjectExplorer.ObjectKind.Procedure)],
                    [new ObjectExplorer.ObjectNodeModel("dbo.FormatName", "FormatName", "sql-main", "applicationdb", "dbo", "FormatName", ObjectExplorer.ObjectKind.Function)],
                    [new ObjectExplorer.ObjectNodeModel("dbo.UsersAudit", "UsersAudit", "sql-main", "applicationdb", "dbo", "UsersAudit", ObjectExplorer.ObjectKind.Trigger)],
                    [],
                    [],
                    [new ObjectExplorer.SecurityNodeModel("dbo", "dbo", ObjectExplorer.SecurityKind.Schema)] )
            ])
        ];
}
