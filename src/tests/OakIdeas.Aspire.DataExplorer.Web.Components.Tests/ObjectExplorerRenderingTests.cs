using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;
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

        ExpandNode(component, "sql-main");
        ExpandNode(component, "applicationdb");
        ExpandNode(component, "Tables");
        ExpandNode(component, "Views");
        ExpandNode(component, "Programmability");
        ExpandNode(component, "Stored Procedures");
        ExpandNode(component, "Functions");
        ExpandNode(component, "Triggers");
        ExpandNode(component, "Security");
        ExpandNode(component, "Schemas");

        component.Markup.Should().Contain("Tables");
        component.Markup.Should().Contain("Views");
        component.Markup.Should().Contain("Programmability");
        component.Markup.Should().Contain("Stored Procedures");
        component.Markup.Should().Contain("Functions");
        component.Markup.Should().Contain("Triggers");
        component.Markup.Should().Contain("Security");
        component.Markup.Should().Contain("Schemas");
        component.Markup.Should().Contain("dbo.Users");
        component.Markup.Should().Contain("dbo.ActiveUsers");
        component.Markup.Should().Contain("dbo.SyncUsers");
        component.Markup.Should().Contain("dbo.FormatName");
        component.Markup.Should().Contain("dbo.UsersAudit");
        component.Markup.Should().Contain("dbo");
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

        ExpandNode(component, "sql-main");
        ExpandNode(component, "applicationdb");
        ExpandNode(component, "Tables");

        component.FindAll(".tree-node")
            .First(node => node.TextContent.Contains("dbo.Users", StringComparison.Ordinal))
            .Click();

        selected.Should().NotBeNull();
        selected!.SchemaName.Should().Be("dbo");
        selected.ObjectName.Should().Be("Users");
        selected.ObjectKind.Should().Be(ObjectExplorer.ObjectKind.Table);
    }

    [Fact]
    public void ContextMenuFlipsLeftAndUpNearViewportEdge()
    {
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.Connections, CreateConnections()));

        ExpandNode(component, "sql-main");
        ExpandNode(component, "applicationdb");
        ExpandNode(component, "Tables");

        var tableNode = component.FindAll(".tree-node")
            .First(node => node.TextContent.Contains("dbo.Users", StringComparison.Ordinal));
        tableNode.TriggerEvent("oncontextmenu", new MouseEventArgs { ClientX = 1300, ClientY = 760 });

        var menu = component.Find(".context-menu");
        menu.ClassList.Should().Contain("context-menu--left");
        menu.ClassList.Should().Contain("context-menu--up");
    }

    [Fact]
    public void ContextMenuOpensDefaultDirectionWhenSpaceIsAvailable()
    {
        var component = RenderComponent<ObjectExplorer>(parameters => parameters
            .Add(p => p.Connections, CreateConnections()));

        ExpandNode(component, "sql-main");
        ExpandNode(component, "applicationdb");
        ExpandNode(component, "Tables");

        var tableNode = component.FindAll(".tree-node")
            .First(node => node.TextContent.Contains("dbo.Users", StringComparison.Ordinal));
        tableNode.TriggerEvent("oncontextmenu", new MouseEventArgs { ClientX = 120, ClientY = 120 });

        var menu = component.Find(".context-menu");
        menu.ClassList.Should().NotContain("context-menu--left");
        menu.ClassList.Should().NotContain("context-menu--up");
    }

    private static IReadOnlyList<ObjectExplorer.ConnectionNode> CreateConnections()
        =>
        [
            new ObjectExplorer.ConnectionNode("sql-main",
            [
                new ObjectExplorer.DatabaseNode("applicationdb",
                [
                    CreateFolder("tables", "Tables", HeroIconKind.TableCells,
                    [
                        CreateObjectLeaf("dbo.Users", "sql-main", "applicationdb", "dbo", "Users", ObjectExplorer.ObjectKind.Table, HeroIconKind.TableCells)
                    ]),
                    CreateFolder("views", "Views", HeroIconKind.QueueList,
                    [
                        CreateObjectLeaf("dbo.ActiveUsers", "sql-main", "applicationdb", "dbo", "ActiveUsers", ObjectExplorer.ObjectKind.View, HeroIconKind.QueueList)
                    ]),
                    CreateFolder("programmability", "Programmability", HeroIconKind.Folder,
                    [
                        CreateFolder("stored-procedures", "Stored Procedures", HeroIconKind.Play,
                        [
                            CreateObjectLeaf("dbo.SyncUsers", "sql-main", "applicationdb", "dbo", "SyncUsers", ObjectExplorer.ObjectKind.Procedure, HeroIconKind.Play)
                        ]),
                        CreateFolder("functions", "Functions", HeroIconKind.CodeBracket,
                        [
                            CreateObjectLeaf("dbo.FormatName", "sql-main", "applicationdb", "dbo", "FormatName", ObjectExplorer.ObjectKind.Function, HeroIconKind.CodeBracket)
                        ]),
                        CreateFolder("triggers", "Triggers", HeroIconKind.Link,
                        [
                            CreateObjectLeaf("dbo.UsersAudit", "sql-main", "applicationdb", "dbo", "UsersAudit", ObjectExplorer.ObjectKind.Trigger, HeroIconKind.Link)
                        ])
                    ]),
                    CreateFolder("security", "Security", HeroIconKind.Folder,
                    [
                        new ObjectExplorer.ExplorerNodeModel("schemas", "Schemas", HeroIconKind.Folder,
                        [
                            new ObjectExplorer.ExplorerNodeModel("schemas/dbo", "dbo", HeroIconKind.Folder, [])
                        ])
                    ])
                ])
            ])
        ];

    private static ObjectExplorer.ExplorerNodeModel CreateFolder(
        string key,
        string label,
        HeroIconKind icon,
        IReadOnlyList<ObjectExplorer.ExplorerNodeModel> children)
        => new(key, label, icon, children);

    private static ObjectExplorer.ExplorerNodeModel CreateObjectLeaf(
        string label,
        string connectionName,
        string databaseName,
        string schemaName,
        string objectName,
        ObjectExplorer.ObjectKind kind,
        HeroIconKind icon)
        => new(
            $"{kind}:{label}",
            label,
            icon,
            [],
            new ObjectExplorer.ObjectSelection(
                connectionName,
                databaseName,
                schemaName,
                label,
                objectName,
                kind));

    private static void ExpandNode(IRenderedComponent<ObjectExplorer> component, string label)
    {
        var node = component.FindAll(".tree-node")
            .First(item => string.Equals(
                item.QuerySelector(".tree-node__label")?.TextContent.Trim(),
                label,
                StringComparison.Ordinal));
        var toggle = node.QuerySelector("button.tree-node__toggle");
        if (string.Equals(toggle?.GetAttribute("aria-label"), "Expand", StringComparison.Ordinal))
        {
            toggle!.Click();
        }
    }
}
