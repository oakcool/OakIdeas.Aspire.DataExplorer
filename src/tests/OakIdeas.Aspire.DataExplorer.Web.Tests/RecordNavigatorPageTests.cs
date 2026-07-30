using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class RecordNavigatorPageTests : BunitContext
{
    private void RegisterServices(bool flagEnabled, IRelationshipNavigatorService? navigatorService = null)
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(flagEnabled));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<IRelationshipNavigatorService>(
            navigatorService ?? new FakeRelationshipNavigatorService());
    }

    [Fact]
    public void DisabledFlag_ShowsUnavailableMessage()
    {
        RegisterServices(false);

        var component = Render<RecordNavigatorPage>();

        component.Markup.Should().Contain("Record Navigator Unavailable");
    }

    [Fact]
    public void EnabledFlag_ShowsLoadRelationshipsButton()
    {
        RegisterServices(true);

        var component = Render<RecordNavigatorPage>();

        component.Markup.Should().Contain("Load Relationships");
    }

    [Fact]
    public void EnabledFlag_ShowsSchemaAndTableInputs()
    {
        RegisterServices(true);

        var component = Render<RecordNavigatorPage>();

        component.Markup.Should().Contain("Schema");
        component.Markup.Should().Contain("Table");
    }

    [Fact]
    public void FlagDisabledAtRuntime_ReRendersUnavailableMessage()
    {
        RegisterServices(true);

        var component = Render<RecordNavigatorPage>();
        component.Markup.Should().Contain("Record Navigator");

        var flagService = Services.GetRequiredService<FeatureFlagStateService>();
        flagService.SetOverride(FeatureKeys.NavigatorRelationshipAwareNavigator, false);

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Record Navigator Unavailable"));
    }

    [Fact]
    public void EnabledFlag_WithRelationships_ShowsRelationshipCards()
    {
        var fakeService = new FakeRelationshipNavigatorService(
        [
            new TableRelationship
            {
                ConstraintName = "FK_Orders_Customers",
                Kind = RelationshipKind.Parent,
                RelatedSchemaName = "dbo",
                RelatedTableName = "Customers",
                ColumnMappings = [new RelationshipColumnMapping("CustomerId", "Id")],
                IsEnforced = true,
            },
        ]);

        RegisterServices(true, fakeService);

        var component = Render<RecordNavigatorPage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Load Relationships"));

        // Enter schema and table
        component.FindAll("input[type='text']")[0].Input("dbo");
        component.FindAll("input[type='text']")[1].Input("Orders");

        // Click Load Relationships
        component.FindAll("button")
            .First(b => b.TextContent.Contains("Load Relationships"))
            .Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("FK_Orders_Customers");
            component.Markup.Should().Contain("Customers");
            component.Markup.Should().Contain("Parent");
        });
    }

    [Fact]
    public void EnabledFlag_WithNoRelationships_ShowsEmptyMessage()
    {
        RegisterServices(true, new FakeRelationshipNavigatorService([]));

        var component = Render<RecordNavigatorPage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Load Relationships"));

        component.FindAll("input[type='text']")[1].Input("EmptyTable");

        component.FindAll("button")
            .First(b => b.TextContent.Contains("Load Relationships"))
            .Click();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("No navigable relationships found"));
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
                IsEnabled = string.Equals(feature.Key, FeatureKeys.NavigatorRelationshipAwareNavigator, StringComparison.Ordinal)
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
                string.Equals(feature.Key, FeatureKeys.NavigatorRelationshipAwareNavigator, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }

    private sealed class FakeRelationshipNavigatorService(
        IReadOnlyList<TableRelationship>? relationships = null) : IRelationshipNavigatorService
    {
        private readonly IReadOnlyList<TableRelationship> _relationships = relationships ?? [];

        public Task<DiscoverTableRelationshipsResponse> DiscoverRelationshipsAsync(
            DiscoverTableRelationshipsRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTableRelationshipsResponse(_relationships));

        public Task<GetRelatedRecordCountResponse> GetRelatedRecordCountAsync(
            GetRelatedRecordCountRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new GetRelatedRecordCountResponse(0));

        public Task<NavigateRelatedRecordsResponse> NavigateRelatedRecordsAsync(
            NavigateRelatedRecordsRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new NavigateRelatedRecordsResponse([], 0, false, string.Empty));
    }
}
