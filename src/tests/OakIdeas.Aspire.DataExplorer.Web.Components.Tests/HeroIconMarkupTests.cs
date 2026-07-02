using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class HeroIconMarkupTests
{
    [Fact]
    public void AllHeroIcons_HaveSvgMarkup()
    {
        var icons = Enum.GetValues<HeroIconKind>();

        foreach (var icon in icons)
        {
            var markup = HeroIconMarkup.GetInnerSvg(icon);

            markup.Should().NotBeNullOrWhiteSpace();
            markup.Should().Contain("<path");
        }
    }

    [Fact]
    public void ServerStackIcon_UsesDistinctHeroiconMarkup()
    {
        var markup = HeroIconMarkup.GetInnerSvg(HeroIconKind.ServerStack);

        markup.Should().Contain("M5.25 14.25h13.5");
        markup.Should().Contain("L5.737 5.1");
    }
}
