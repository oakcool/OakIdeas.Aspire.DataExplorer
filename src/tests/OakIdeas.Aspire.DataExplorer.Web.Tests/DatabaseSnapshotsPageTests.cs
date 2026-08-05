using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class DatabaseSnapshotsPageTests : BunitContext
{
    private void RegisterServices(bool featureEnabled = true, string? selectedDatabase = "TestDb")
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(featureEnabled));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<ISnapshotService>(new InMemorySnapshotService());
        Services.AddSingleton<ISelectedDatabaseService>(new FakeSelectedDatabaseService(selectedDatabase));
    }

    [Fact]
    public void WhenFeatureDisabled_ShowsUnavailableMessage()
    {
        RegisterServices(featureEnabled: false);

        var component = Render<DatabaseSnapshotsPage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Database Snapshots Unavailable"));
    }

    [Fact]
    public void WhenFeatureEnabled_ShowsPageHeader()
    {
        RegisterServices(featureEnabled: true);

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().Contain("Database Snapshots");
        component.Markup.Should().Contain("New Snapshot");
    }

    [Fact]
    public void WhenNoDatabaseSelected_ShowsSelectDatabaseMessage()
    {
        RegisterServices(featureEnabled: true, selectedDatabase: null);

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().Contain("Select a database");
    }

    [Fact]
    public void WhenDatabaseSelectedAndNoSnapshots_ShowsEmptyState()
    {
        RegisterServices(featureEnabled: true, selectedDatabase: "MyDb");

        var component = Render<DatabaseSnapshotsPage>();

        component.Markup.Should().Contain("No snapshots yet");
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

    private sealed class FakeSelectedDatabaseService(string? databaseName) : ISelectedDatabaseService
    {
        public event EventHandler<SelectedDatabaseContext?>? SelectionChanged;

        public Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
            => Task.FromResult(new SelectDatabaseResponse(false, null, null));

        public Task<SelectedDatabaseContext?> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
        {
            if (databaseName is null)
            {
                return Task.FromResult<SelectedDatabaseContext?>(null);
            }

            var resource = new DiscoveredDatabaseResource(
                ResourceId: "test-resource",
                ResourceName: "test-resource",
                DatabaseName: databaseName,
                ProviderType: DatabaseProviderType.SqlServer,
                ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>()),
                IsAvailable: true,
                DiscoveredAt: DateTimeOffset.UtcNow);

            return Task.FromResult<SelectedDatabaseContext?>(
                new SelectedDatabaseContext(resource, true, null));
        }

        public Task ClearSelectionAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> IsSelectedAsync(CancellationToken cancellationToken)
            => Task.FromResult(databaseName is not null);
    }
}
