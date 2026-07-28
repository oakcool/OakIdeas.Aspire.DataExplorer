using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class SettingsPageTests : BunitContext
{
    public SettingsPageTests()
    {
        Services.AddSingleton<IFeatureFlagService, AllEnabledFeatureFlagService>();
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(
        [
            new FeatureFlag
            {
                Key = "Stub.Alpha",
                DisplayName = "Alpha setting",
                Description = "Alpha description",
                Category = FeatureCategory.Query,
                DefaultEnabled = true,
            },
        ]));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddScoped<ISettingsSectionProvider, FeatureFlagsSettingsSectionProvider>();
    }

    [Fact]
    public void SettingsPage_DefaultRoute_RendersFeatureFlagsSection()
    {
        var component = Render<SettingsPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Settings");
            component.Markup.Should().Contain("Feature Flags");
            component.Markup.Should().Contain("Override feature evaluations for this session");
        });
    }

    [Fact]
    public void SettingsPage_SearchResults_LinkToFeatureFlagsSectionAnchors()
    {
        var component = Render<SettingsPage>();

        component.Find("input[aria-label='Search settings']").Input("alpha");

        component.WaitForAssertion(() =>
        {
            var link = component.Find(".de-settings-search-results__item");
            link.GetAttribute("href").Should().Be("/settings/feature-flags#feature-flag-stub-alpha");
        });
    }

    [Fact]
    public void FeatureFlagsPage_LegacyRoute_RedirectsToSettingsSection()
    {
        Render<FeatureFlagsPage>();

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.Should().EndWith("/settings/feature-flags");
    }

    private sealed class AllEnabledFeatureFlagService : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(FeatureFlag feature, FeatureEvaluationContext context, CancellationToken cancellationToken = default)
            => new(new FeatureFlagResult
            {
                Key = feature.Key,
                IsEnabled = true,
                WinningSource = "test",
                UsedCatalogDefault = false,
                EvaluationTrace =
                [
                    FeatureFlagSourceResult.Enabled("test"),
                ],
            });

        public ValueTask<bool> IsEnabledAsync(FeatureFlag feature, FeatureEvaluationContext? context = null, CancellationToken cancellationToken = default)
            => new(true);
    }
}
