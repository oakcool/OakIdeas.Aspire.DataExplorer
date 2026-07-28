using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class FeatureFlagServiceTests
{
    private static readonly FeatureFlag TestFeature = new()
    {
        Key = "Test.Feature",
        DisplayName = "Test Feature",
        Description = "A test feature",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
    };

    private static readonly FeatureFlag DefaultDisabledFeature = new()
    {
        Key = "Test.DefaultDisabled",
        DisplayName = "Test Disabled Feature",
        Description = "A feature that defaults to disabled",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = false,
    };

    [Fact]
    public async Task EvaluateAsync_WhenNoSources_UsesCatalogDefault_Enabled()
    {
        var service = CreateService([], new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeTrue();
        result.UsedCatalogDefault.Should().BeTrue();
        result.WinningSource.Should().Be("CatalogDefault");
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoSources_UsesCatalogDefault_Disabled()
    {
        var service = CreateService([], new FeatureFlagOptions());

        var result = await service.EvaluateAsync(DefaultDisabledFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeFalse();
        result.UsedCatalogDefault.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceReturnsEnabled_ReturnsEnabled()
    {
        var source = new StubSourceProvider("TestSource", FeatureFlagSourceResult.Enabled("TestSource"));
        var service = CreateService([new OrderedSourceProvider(100, source)], new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeTrue();
        result.UsedCatalogDefault.Should().BeFalse();
        result.WinningSource.Should().Be("TestSource");
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceReturnsDisabled_ReturnsDisabled()
    {
        var source = new StubSourceProvider("TestSource", FeatureFlagSourceResult.Disabled("TestSource"));
        var service = CreateService([new OrderedSourceProvider(100, source)], new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeFalse();
        result.UsedCatalogDefault.Should().BeFalse();
        result.WinningSource.Should().Be("TestSource");
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceReturnsNotDefined_FallsBackToCatalogDefault()
    {
        var source = new StubSourceProvider("TestSource", FeatureFlagSourceResult.NotDefined("TestSource"));
        var service = CreateService([new OrderedSourceProvider(100, source)], new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeTrue();
        result.UsedCatalogDefault.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhenHighPrioritySourceWins_LowerPriorityIsNotConsidered()
    {
        var highPriority = new StubSourceProvider("High", FeatureFlagSourceResult.Disabled("High"));
        var lowPriority = new StubSourceProvider("Low", FeatureFlagSourceResult.Enabled("Low"));

        var service = CreateService(
            [
                new OrderedSourceProvider(200, lowPriority),
                new OrderedSourceProvider(100, highPriority),
            ],
            new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeFalse();
        result.WinningSource.Should().Be("High");
        result.EvaluationTrace.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_WhenFirstSourceNotDefined_FallsBackToSecondSource()
    {
        var first = new StubSourceProvider("First", FeatureFlagSourceResult.NotDefined("First"));
        var second = new StubSourceProvider("Second", FeatureFlagSourceResult.Enabled("Second"));

        var service = CreateService(
            [
                new OrderedSourceProvider(100, first),
                new OrderedSourceProvider(200, second),
            ],
            new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeTrue();
        result.WinningSource.Should().Be("Second");
        result.EvaluationTrace.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceThrows_ContinuesToNextSourceAndAddsWarning()
    {
        var throwing = new ThrowingSourceProvider("Throwing");
        var fallback = new StubSourceProvider("Fallback", FeatureFlagSourceResult.Enabled("Fallback"));

        var service = CreateService(
            [
                new OrderedSourceProvider(100, throwing),
                new OrderedSourceProvider(200, fallback),
            ],
            new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeTrue();
        result.WinningSource.Should().Be("Fallback");
        result.Warnings.Should().ContainSingle(w => w.Contains("Throwing"));
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceUnavailable_AddsWarningAndContinues()
    {
        var unavailable = new StubSourceProvider("Unavailable",
            FeatureFlagSourceResult.Unavailable("Unavailable", "connection lost"));
        var fallback = new StubSourceProvider("Fallback", FeatureFlagSourceResult.Disabled("Fallback"));

        var service = CreateService(
            [
                new OrderedSourceProvider(100, unavailable),
                new OrderedSourceProvider(200, fallback),
            ],
            new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeFalse();
        result.Warnings.Should().ContainSingle(w => w.Contains("Unavailable"));
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceInvalidValue_AddsWarningAndContinues()
    {
        var invalid = new StubSourceProvider("Config",
            FeatureFlagSourceResult.Invalid("Config", "value 'yes' is not a boolean"));
        var service = CreateService([new OrderedSourceProvider(100, invalid)], new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeTrue(); // falls back to catalog default
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public async Task EvaluateAsync_WhenFailClosedAndNoSources_ReturnsDisabled()
    {
        var options = new FeatureFlagOptions
        {
            DefaultFailureBehavior = FeatureFlagFailureBehavior.FailClosed,
        };
        var service = CreateService([], options);

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.IsEnabled.Should().BeFalse();
        result.UsedCatalogDefault.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IncludesAllConsultedSourcesInTrace()
    {
        var first = new StubSourceProvider("First", FeatureFlagSourceResult.NotDefined("First"));
        var second = new StubSourceProvider("Second", FeatureFlagSourceResult.NotDefined("Second"));

        var service = CreateService(
            [
                new OrderedSourceProvider(100, first),
                new OrderedSourceProvider(200, second),
            ],
            new FeatureFlagOptions());

        var result = await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.EvaluationTrace.Should().HaveCount(2);
        result.EvaluationTrace.Select(t => t.SourceName).Should().Contain("First");
        result.EvaluationTrace.Select(t => t.SourceName).Should().Contain("Second");
    }

    [Fact]
    public async Task IsEnabledAsync_WhenSourceEnabled_ReturnsTrue()
    {
        var source = new StubSourceProvider("Source", FeatureFlagSourceResult.Enabled("Source"));
        var service = CreateService([new OrderedSourceProvider(100, source)], new FeatureFlagOptions());

        var result = await service.IsEnabledAsync(TestFeature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenSourceDisabled_ReturnsFalse()
    {
        var source = new StubSourceProvider("Source", FeatureFlagSourceResult.Disabled("Source"));
        var service = CreateService([new OrderedSourceProvider(100, source)], new FeatureFlagOptions());

        var result = await service.IsEnabledAsync(TestFeature);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var source = new DelayedSourceProvider("Slow");
        var service = CreateService([new OrderedSourceProvider(100, source)], new FeatureFlagOptions());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await service.EvaluateAsync(TestFeature, FeatureEvaluationContext.Empty, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static FeatureFlagService CreateService(
        IEnumerable<OrderedSourceProvider> sources,
        FeatureFlagOptions options)
    {
        var catalog = new FeatureFlagCatalog(ApplicationFeatures.All);
        return new FeatureFlagService(
            catalog,
            sources,
            Options.Create(options),
            NullLogger<FeatureFlagService>.Instance);
    }

    private sealed class StubSourceProvider(string name, FeatureFlagSourceResult result) : IFeatureFlagSourceProvider
    {
        public string Name => name;

        public ValueTask<FeatureFlagSourceResult> TryGetAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(result);
    }

    private sealed class ThrowingSourceProvider(string name) : IFeatureFlagSourceProvider
    {
        public string Name => name;

        public ValueTask<FeatureFlagSourceResult> TryGetAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Source is broken.");
    }

    private sealed class DelayedSourceProvider(string name) : IFeatureFlagSourceProvider
    {
        public string Name => name;

        public async ValueTask<FeatureFlagSourceResult> TryGetAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return FeatureFlagSourceResult.Enabled(Name);
        }
    }
}
