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

public sealed class RequestTracePageTests : BunitContext
{
    private static CorrelatedSpan MakeSpan(
        string spanId = "span001",
        string traceId = "trace001",
        string serviceName = "test-svc",
        string dbName = "TestDb",
        string? statement = "SELECT * FROM Orders",
        SpanStatusCode status = SpanStatusCode.Ok,
        double durationMs = 50.0) => new(
        SpanId: spanId,
        TraceId: traceId,
        ServiceName: serviceName,
        DbSystem: "mssql",
        DbName: dbName,
        DbStatement: statement,
        PeerAddress: "localhost:1433",
        StartTime: DateTimeOffset.UtcNow,
        Duration: TimeSpan.FromMilliseconds(durationMs),
        StatusCode: status,
        ErrorMessage: null);

    private void RegisterServices(bool flagEnabled, ITraceCorrelationService? traceService = null)
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(flagEnabled));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<ITraceCorrelationService>(
            traceService ?? new InMemoryTraceCorrelationService([]));
        Services.AddSingleton<ITraceInsightsAnalyzer, TraceInsightsAnalyzer>();
    }

    [Fact]
    public void DisabledFlag_ShowsUnavailableMessage()
    {
        RegisterServices(false);

        var component = Render<RequestTracePage>();

        component.Markup.Should().Contain("Request Trace Unavailable");
    }

    [Fact]
    public void EnabledFlag_WithNoSpans_ShowsEmptyState()
    {
        RegisterServices(true);

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Request Trace");
            component.Markup.Should().Contain("No spans ingested yet");
        });
    }

    [Fact]
    public void EnabledFlag_WithSpans_ShowsSpanList()
    {
        var store = new InMemoryTraceCorrelationService([]);
        store.IngestSpan(MakeSpan(serviceName: "my-api", dbName: "SalesDb", statement: "SELECT 1"));
        RegisterServices(true, store);

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("my-api");
            component.Markup.Should().Contain("SalesDb");
        });
    }

    [Fact]
    public void EnabledFlag_WithSpans_MasksSqlStatement()
    {
        var store = new InMemoryTraceCorrelationService([]);
        store.IngestSpan(MakeSpan(statement: "SELECT * FROM Products WHERE Name = 'Widget'"));
        RegisterServices(true, store);

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().NotContain("Widget");
        });
    }

    [Fact]
    public void EnabledFlag_ShowsSpanCount()
    {
        var store = new InMemoryTraceCorrelationService([]);
        store.IngestSpan(MakeSpan());
        store.IngestSpan(MakeSpan(spanId: "span002"));
        RegisterServices(true, store);

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("2 span(s)");
        });
    }

    [Fact]
    public void FlagDisabledAtRuntime_ReRendersUnavailableMessage()
    {
        RegisterServices(true);

        var component = Render<RequestTracePage>();
        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Request Trace"));

        var flagService = Services.GetRequiredService<FeatureFlagStateService>();
        flagService.SetOverride(FeatureKeys.TelemetryRequestTrace, false);

        component.WaitForAssertion(() =>
            component.Markup.Should().Contain("Request Trace Unavailable"));
    }

    [Fact]
    public void InsightsBar_ShownWhenDiagnosticsDetected()
    {
        var store = new InMemoryTraceCorrelationService([]);

        // Insert enough spans to trigger a repeated-query insight.
        for (var i = 0; i < TraceInsightsAnalyzer.RepeatedQueryThreshold; i++)
        {
            store.IngestSpan(MakeSpan(
                spanId: $"span{i}",
                statement: "SELECT * FROM Orders"));
        }

        RegisterServices(true, store);

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Diagnostic Insights");
            component.Markup.Should().Contain("Repeated Query");
        });
    }

    [Fact]
    public void InsightsBar_NotShownWhenNoInsights()
    {
        var store = new InMemoryTraceCorrelationService([]);
        store.IngestSpan(MakeSpan());
        RegisterServices(true, store);

        var component = Render<RequestTracePage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().NotContain("Diagnostic Insights");
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

