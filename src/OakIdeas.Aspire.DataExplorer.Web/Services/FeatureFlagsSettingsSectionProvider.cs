using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Models;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

public sealed class FeatureFlagsSettingsSectionProvider(IFeatureFlagCatalog catalog) : ISettingsSectionProvider
{
    private const string SectionId = "feature-flags";
    private const string SectionTitle = "Feature Flags";

    public IReadOnlyList<SettingsSectionDefinition> GetSections()
        =>
        [
            new SettingsSectionDefinition(SectionId, SectionTitle, HeroIconKind.Cog6Tooth, typeof(FeatureFlagsSettingsSection)),
        ];

    public IReadOnlyList<SettingsSearchResultDefinition> GetSearchResults(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return [];
        }

        var trimmedTerm = term.Trim();

        return catalog.Features
            .Where(feature =>
                feature.DisplayName.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase)
                || feature.Description.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase)
                || feature.Key.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(feature => feature.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(feature => new SettingsSearchResultDefinition(
                feature.DisplayName,
                feature.Description,
                $"/settings/{SectionId}#{FeatureFlagAnchorBuilder.Build(feature.Key)}",
                SectionTitle))
            .ToList();
    }
}
