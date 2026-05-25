using Bunit;
using OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ExplorerContextMenuRenderingTests : TestContext
{
    [Fact]
    public void RendersNothingWhenNotVisible()
    {
        var items = new List<ExplorerContextMenuItem>
        {
            new() { Id = "view", Label = "View" }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, false));

        component.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void RendersMenuItemsWhenVisible()
    {
        var items = new List<ExplorerContextMenuItem>
        {
            new() { Id = "view", Label = "View" },
            ExplorerContextMenuItem.Separator,
            new() { Id = "select-top-1000", Label = "Select TOP 1000 Rows" }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true)
            .Add(p => p.PositionX, 100)
            .Add(p => p.PositionY, 200));

        component.Markup.Should().Contain("View");
        component.Markup.Should().Contain("Select TOP 1000 Rows");
    }

    [Fact]
    public void RendersAtCorrectPosition()
    {
        var items = new List<ExplorerContextMenuItem>
        {
            new() { Id = "view", Label = "View" }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true)
            .Add(p => p.PositionX, 150.5)
            .Add(p => p.PositionY, 300.25));

        component.Markup.Should().Contain("left:clamp(8px, 150.5px, calc(100vw - 228px))");
        component.Markup.Should().Contain("top:clamp(8px, 300.25px, calc(100vh - 8px))");
    }

    [Fact]
    public void RendersSeparator()
    {
        var items = new List<ExplorerContextMenuItem>
        {
            new() { Id = "view", Label = "View" },
            ExplorerContextMenuItem.Separator,
            new() { Id = "other", Label = "Other" }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true));

        component.FindAll(".context-menu__separator").Should().HaveCount(1);
    }

    [Fact]
    public void DisabledItemRendersWithDisabledClass()
    {
        var items = new List<ExplorerContextMenuItem>
        {
            new() { Id = "disabled-action", Label = "Unavailable", IsEnabled = false }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true));

        component.Find(".context-menu__item").ClassList.Should().Contain("context-menu__item--disabled");
    }

    [Fact]
    public async Task ClickingItemInvokesAction()
    {
        bool actionInvoked = false;
        var items = new List<ExplorerContextMenuItem>
        {
            new()
            {
                Id = "view",
                Label = "View",
                Action = () =>
                {
                    actionInvoked = true;
                    return Task.CompletedTask;
                }
            }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true));

        await component.InvokeAsync(() => component.Find(".context-menu__item").Click());

        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task ClickingItemDismissesMenu()
    {
        bool dismissed = false;
        var items = new List<ExplorerContextMenuItem>
        {
            new()
            {
                Id = "view",
                Label = "View",
                Action = () => Task.CompletedTask
            }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnDismiss, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => dismissed = true)));

        await component.InvokeAsync(() => component.Find(".context-menu__item").Click());

        dismissed.Should().BeTrue();
    }

    [Fact]
    public async Task ClickingBackdropDismissesMenu()
    {
        bool dismissed = false;
        var items = new List<ExplorerContextMenuItem>
        {
            new() { Id = "view", Label = "View" }
        };

        var component = RenderComponent<ExplorerContextMenu>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnDismiss, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => dismissed = true)));

        await component.InvokeAsync(() => component.Find(".context-menu-backdrop").Click());

        dismissed.Should().BeTrue();
    }
}
