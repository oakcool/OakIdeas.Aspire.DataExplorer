using OakIdeas.Aspire.DataExplorer.Web.Models;

namespace OakIdeas.Aspire.DataExplorer.Web.Abstractions;

public interface ISettingsSectionProvider
{
    IReadOnlyList<SettingsSectionDefinition> GetSections();

    IReadOnlyList<SettingsSearchResultDefinition> GetSearchResults(string term);
}
