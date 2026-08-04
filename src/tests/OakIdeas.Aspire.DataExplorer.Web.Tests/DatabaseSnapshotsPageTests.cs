using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class DatabaseSnapshotsPageTests : BunitContext
{
    private void RegisterServices(bool flagEnabled)
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(flagEnabled));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
    }

    [Fact]
    public void DisabledFlag_ShowsUnavailableMessage()
    {
        RegisterServices(false);

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().Contain("Database Snapshots Unavailable");
    }

    [Fact]
    public void DisabledFlag_DoesNotShowSnapshotsPageContent()
    {
        RegisterServices(false);

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().NotContain("Full implementation is in progress");
    }

    [Fact]
    public void EnabledFlag_ShowsPageHeading()
    {
        RegisterServices(true);

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().Contain("Database Snapshots");
    }

    [Fact]
    public void EnabledFlag_ShowsPreviewBadge()
    {
        RegisterServices(true);

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().Contain("preview");
    }

    private sealed class FixedFeatureFlagService(bool enabled) : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagResult
            {
                Key = feature.Key,
                IsEnabled = string.Equals(feature.Key, FeatureKeys.SnapshotsDatabaseSnapshots, StringComparison.Ordinal)
                    ? enabled
                    : true,
                WinningSource = "Test",
                UsedCatalogDefault = false,
                EvaluationTrace = [],
            });

        public ValueTask<bool> IsEnabledAsync(
            FeatureFlag feature,
            FeatureEvaluationContext? context = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                string.Equals(feature.Key, FeatureKeys.SnapshotsDatabaseSnapshots, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }
}
