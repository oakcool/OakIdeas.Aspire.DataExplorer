using OakIdeas.Aspire.DataExplorer.Sample.Web.Components.Components.Atoms;

namespace OakIdeas.Aspire.DataExplorer.Sample.Web.Components.Tests;

public sealed class TopNavLinkTests
{
    [Fact]
    public void NavLinkItem_StoresLabelAndHref()
    {
        var link = new TopNav.NavLinkItem("Home", "/");

        link.Label.Should().Be("Home");
        link.Href.Should().Be("/");
    }

    [Fact]
    public void NavLinkItem_DefaultsIconToEmpty()
    {
        var link = new TopNav.NavLinkItem("Home", "/");

        link.Icon.Should().BeEmpty();
    }

    [Fact]
    public void NavLinkItem_DefaultsExactToFalse()
    {
        var link = new TopNav.NavLinkItem("Home", "/");

        link.Exact.Should().BeFalse();
    }

    [Fact]
    public void NavLinkItem_WithIconAndExact()
    {
        var link = new TopNav.NavLinkItem("Home", "/", "🏠", Exact: true);

        link.Icon.Should().Be("🏠");
        link.Exact.Should().BeTrue();
    }

    [Fact]
    public void NavLinkItem_MultipleLinks_AreIndependent()
    {
        var links = new[]
        {
            new TopNav.NavLinkItem("Home", "/", "🏠", Exact: true),
            new TopNav.NavLinkItem("Todos", "/todos", "✅"),
        };

        links[0].Label.Should().Be("Home");
        links[0].Exact.Should().BeTrue();
        links[1].Label.Should().Be("Todos");
        links[1].Exact.Should().BeFalse();
    }
}
