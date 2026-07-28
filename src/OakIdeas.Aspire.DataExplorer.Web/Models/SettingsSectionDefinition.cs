using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;

namespace OakIdeas.Aspire.DataExplorer.Web.Models;

public sealed record SettingsSectionDefinition(
    string Id,
    string Title,
    HeroIconKind Icon,
    Type ComponentType)
{
    public string Href => $"/settings/{Id}";
}
