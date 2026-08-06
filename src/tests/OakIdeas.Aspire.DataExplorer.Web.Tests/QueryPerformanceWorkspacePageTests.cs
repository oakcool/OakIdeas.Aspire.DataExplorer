using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class QueryPerformanceWorkspacePageTests : BunitContext
{
    private void RegisterServices(bool featureEnabled = true, IQueryPerformanceService? performanceService = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(featureEnabled));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<IQueryPerformanceService>(performanceService ?? new FakeUnsupportedQueryPerformanceService());
    }

    [Fact]
    public void WhenFeatureDisabled_ShowsUnavailableMessage()
    {
        RegisterServices(featureEnabled: false);

        var component = Render<QueryPerformanceWorkspacePage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Query Performance Workspace Unavailable"));
    }

    [Fact]
    public void WhenFeatureEnabled_ShowsPageHeader()
    {
        RegisterServices(featureEnabled: true);

        var component = Render<QueryPerformanceWorkspacePage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Query Performance"));
    }

    [Fact]
    public void WhenProviderUnsupported_ShowsUnavailableNotice()
    {
        RegisterServices(featureEnabled: true, new FakeUnsupportedQueryPerformanceService());

        var component = Render<QueryPerformanceWorkspacePage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Query performance data unavailable"));
    }

    [Fact]
    public void WhenProviderSupported_WithNoEntries_ShowsEmptyState()
    {
        RegisterServices(featureEnabled: true, new FakeSupportedQueryPerformanceService([]));

        var component = Render<QueryPerformanceWorkspacePage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("No queries match"));
    }

    [Fact]
    public void WhenProviderSupported_WithEntries_ShowsTable()
    {
        var entries = new List<QueryPerformanceEntry>
        {
            new()
            {
                QueryId = "1",
                QueryText = "SELECT * FROM Orders WHERE CustomerId = @p0",
                DatabaseName = "appdb",
                ExecutionCount = 500,
                AvgDurationMs = 45.2,
                TotalDurationMs = 22600,
                MaxDurationMs = 320,
                AvgLogicalReads = 120,
                TotalLogicalReads = 60000,
                AvgLogicalWrites = 0,
                TotalLogicalWrites = 0,
                FailureCount = 0,
                AvgRowCount = 12,
                PlanCount = 1,
                HasRegression = false,
            },
        };

        RegisterServices(featureEnabled: true, new FakeSupportedQueryPerformanceService(entries));

        var component = Render<QueryPerformanceWorkspacePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("SELECT * FROM Orders");
            component.Markup.Should().Contain("500");
        });
    }

    [Fact]
    public void WhenEntryHasRegression_ShowsRegressionIndicator()
    {
        var entries = new List<QueryPerformanceEntry>
        {
            new()
            {
                QueryId = "2",
                QueryText = "SELECT TOP 1000 * FROM Products",
                DatabaseName = "appdb",
                ExecutionCount = 10,
                AvgDurationMs = 4500,
                TotalDurationMs = 45000,
                MaxDurationMs = 5000,
                AvgLogicalReads = 50000,
                TotalLogicalReads = 500000,
                AvgLogicalWrites = 0,
                TotalLogicalWrites = 0,
                FailureCount = 0,
                AvgRowCount = 1000,
                PlanCount = 2,
                HasRegression = true,
            },
        };

        RegisterServices(featureEnabled: true, new FakeSupportedQueryPerformanceService(entries));

        var component = Render<QueryPerformanceWorkspacePage>();

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("⚠"));
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
                IsEnabled = string.Equals(feature.Key, FeatureKeys.PerformanceQueryPerformanceWorkspace, StringComparison.Ordinal)
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
                string.Equals(feature.Key, FeatureKeys.PerformanceQueryPerformanceWorkspace, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }

    private sealed class FakeUnsupportedQueryPerformanceService : IQueryPerformanceService
    {
        public Task<GetQueryPerformanceResponse> GetTopQueriesAsync(
            GetQueryPerformanceRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new GetQueryPerformanceResponse
            {
                Entries = [],
                TotalCount = 0,
                IsSupported = false,
                UnsupportedReason = "Query performance data unavailable for this provider.",
                DataSource = null,
            });
    }

    private sealed class FakeSupportedQueryPerformanceService(IReadOnlyList<QueryPerformanceEntry> entries) : IQueryPerformanceService
    {
        public Task<GetQueryPerformanceResponse> GetTopQueriesAsync(
            GetQueryPerformanceRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new GetQueryPerformanceResponse
            {
                Entries = entries,
                TotalCount = entries.Count,
                IsSupported = true,
                DataSource = "SQL Server Query Store",
            });
    }
}
