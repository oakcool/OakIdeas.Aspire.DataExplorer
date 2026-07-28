using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class RequestTracePageTests : BunitContext
{
    [Fact]
    public void DisabledFlag_ShowsUnavailableMessage()
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(false));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();

        var component = Render<RequestTracePage>();

        component.Markup.Should().Contain("Request Trace Unavailable");
        component.Markup.Should().NotContain("Feature rollout is in progress");
    }

    [Fact]
    public void EnabledFlag_ShowsPlaceholderContent()
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(true));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Request Trace");
            component.Markup.Should().Contain("Feature rollout is in progress");
            component.Markup.Should().NotContain("Request Trace Unavailable");
        });
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
                IsEnabled = string.Equals(feature.Key, FeatureKeys.TelemetryRequestTrace, StringComparison.Ordinal)
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
                string.Equals(feature.Key, FeatureKeys.TelemetryRequestTrace, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }
}
