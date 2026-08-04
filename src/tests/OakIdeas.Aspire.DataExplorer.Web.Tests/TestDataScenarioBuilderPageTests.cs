using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class TestDataScenarioBuilderPageTests : BunitContext
{
    private void RegisterServices(bool flagEnabled, IScenarioBuilderService? service = null)
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(flagEnabled));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<IScenarioBuilderService>(
            service ?? new InMemoryScenarioBuilderService());
    }

    [Fact]
    public void DisabledFlag_ShowsUnavailableMessage()
    {
        RegisterServices(false);

        var component = Render<TestDataScenarioBuilderPage>();

        component.Markup.Should().Contain("Test Data Scenario Builder Unavailable");
    }

    [Fact]
    public void EnabledFlag_ShowsNewScenarioButton()
    {
        RegisterServices(true);

        var component = Render<TestDataScenarioBuilderPage>();

        component.Markup.Should().Contain("New Scenario");
    }

    [Fact]
    public void EnabledFlag_EmptyList_ShowsNoScenariosMessage()
    {
        RegisterServices(true);

        var component = Render<TestDataScenarioBuilderPage>();

        component.Markup.Should().Contain("No scenarios yet");
    }

    [Fact]
    public void EnabledFlag_WithScenarios_ShowsScenarioNamesInList()
    {
        var svc = new InMemoryScenarioBuilderService();
        svc.CreateScenario(new CreateScenarioRequest("Customer Flow", null, null, []));
        svc.CreateScenario(new CreateScenarioRequest("Order Flow", null, null, []));

        RegisterServices(true, svc);

        var component = Render<TestDataScenarioBuilderPage>();

        component.Markup.Should().Contain("Customer Flow");
        component.Markup.Should().Contain("Order Flow");
    }

    [Fact]
    public void ClickNewScenario_ShowsCreateForm()
    {
        RegisterServices(true);

        var component = Render<TestDataScenarioBuilderPage>();

        component.FindAll("button")
            .First(b => b.TextContent.Contains("New Scenario"))
            .Click();

        component.Markup.Should().Contain("New Scenario");
        component.Markup.Should().Contain("Save Scenario");
    }

    [Fact]
    public void CreateForm_SaveWithEmptyName_ShowsError()
    {
        RegisterServices(true);

        var component = Render<TestDataScenarioBuilderPage>();

        component.FindAll("button")
            .First(b => b.TextContent.Contains("New Scenario"))
            .Click();

        // Save button should be disabled when name is empty — verify it is present but disabled
        var saveButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Save Scenario"));

        saveButton.Should().NotBeNull();
        saveButton!.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void FlagDisabledAtRuntime_ReRendersUnavailableMessage()
    {
        RegisterServices(true);

        var component = Render<TestDataScenarioBuilderPage>();
        component.Markup.Should().Contain("New Scenario");

        var flagService = Services.GetRequiredService<FeatureFlagStateService>();
        flagService.SetOverride(FeatureKeys.ScenarioBuilderTestDataScenarioBuilder, false);

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Test Data Scenario Builder Unavailable"));
    }

    [Fact]
    public void SelectScenario_ShowsDetailPanel()
    {
        var svc = new InMemoryScenarioBuilderService();
        svc.CreateScenario(new CreateScenarioRequest("My Scenario", "A helpful description", null, []));

        RegisterServices(true, svc);

        var component = Render<TestDataScenarioBuilderPage>();

        component.FindAll("button")
            .First(b => b.TextContent.Contains("My Scenario"))
            .Click();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Run Scenario");
            component.Markup.Should().Contain("A helpful description");
        });
    }

    [Fact]
    public void RunScenario_Success_ShowsExecutionResult()
    {
        var svc = new InMemoryScenarioBuilderService();
        svc.CreateScenario(new CreateScenarioRequest("Runnable", null, null,
        [
            new ScenarioTableOperation("dbo", "Items", null,
            [
                new ScenarioColumnValue("Name", ScenarioValueKind.Fixed, FixedValue: "test"),
            ]),
        ]));

        RegisterServices(true, svc);

        var component = Render<TestDataScenarioBuilderPage>();

        // Select the scenario
        component.FindAll("button")
            .First(b => b.TextContent.Contains("Runnable"))
            .Click();

        // Run it
        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Run Scenario"));

        component.FindAll("button")
            .First(b => b.TextContent.Contains("Run Scenario"))
            .Click();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Execution succeeded"));
    }

    [Fact]
    public void DeleteScenario_RemovesFromList()
    {
        var svc = new InMemoryScenarioBuilderService();
        svc.CreateScenario(new CreateScenarioRequest("To Delete", null, null, []));

        RegisterServices(true, svc);

        var component = Render<TestDataScenarioBuilderPage>();
        component.Markup.Should().Contain("To Delete");

        // Select it first
        component.FindAll("button")
            .First(b => b.TextContent.Contains("To Delete"))
            .Click();

        // Wait for detail panel to appear with the delete button (title="Delete this scenario")
        component.WaitForAssertion(() =>
            component.FindAll("button")
                .Should().Contain(b => b.GetAttribute("title") == "Delete this scenario"));

        component.FindAll("button")
            .First(b => b.GetAttribute("title") == "Delete this scenario")
            .Click();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("No scenarios yet"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FixedFeatureFlagService(bool enabled) : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagResult
            {
                Key = feature.Key,
                IsEnabled = string.Equals(feature.Key, FeatureKeys.ScenarioBuilderTestDataScenarioBuilder, StringComparison.Ordinal)
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
                string.Equals(feature.Key, FeatureKeys.ScenarioBuilderTestDataScenarioBuilder, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }
}
