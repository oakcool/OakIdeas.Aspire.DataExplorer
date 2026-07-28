using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class FeatureFlagStartupValidatorTests
{
    private static FeatureFlagCatalog BuildCatalog(params FeatureFlag[] features)
        => new(features.ToList().AsReadOnly());

    private static FeatureFlag MakeFeature(string key, string[]? dependsOn = null) => new()
    {
        Key = key,
        DisplayName = key,
        Description = key,
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        DependsOn = dependsOn ?? [],
    };

    [Fact]
    public async Task StartAsync_ValidCatalog_CompletesWithoutError()
    {
        var catalog = BuildCatalog(MakeFeature("A"), MakeFeature("B", ["A"]));
        var service = new AllEnabledFeatureFlagService();
        var validator = new FeatureFlagStartupValidator(catalog, service, NullLogger<FeatureFlagStartupValidator>.Instance);

        var act = async () => await validator.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_CatalogWithUnknownDependency_CompletesWithoutThrowing()
    {
        // Startup validator logs warnings but does not throw for invalid dependencies.
        var catalog = BuildCatalog(MakeFeature("A", ["NonExistent"]));
        var service = new AllEnabledFeatureFlagService();
        var validator = new FeatureFlagStartupValidator(catalog, service, NullLogger<FeatureFlagStartupValidator>.Instance);

        var act = async () => await validator.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_EvaluationThrows_CompletesWithoutRethrowing()
    {
        // The validator should catch evaluation errors and log them without crashing startup.
        var catalog = BuildCatalog(MakeFeature("A"));
        var service = new ThrowingFeatureFlagService();
        var validator = new FeatureFlagStartupValidator(catalog, service, NullLogger<FeatureFlagStartupValidator>.Instance);

        var act = async () => await validator.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_AlwaysCompletes()
    {
        var catalog = BuildCatalog();
        var service = new AllEnabledFeatureFlagService();
        var validator = new FeatureFlagStartupValidator(catalog, service, NullLogger<FeatureFlagStartupValidator>.Instance);

        await validator.StopAsync(CancellationToken.None);
    }

    private sealed class AllEnabledFeatureFlagService : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(
            FeatureFlag feature, FeatureEvaluationContext context, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagResult
            {
                Key = feature.Key,
                IsEnabled = true,
                WinningSource = "CatalogDefault",
                UsedCatalogDefault = true,
                EvaluationTrace = [],
            });

        public ValueTask<bool> IsEnabledAsync(
            FeatureFlag feature, FeatureEvaluationContext? context = null, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);
    }

    private sealed class ThrowingFeatureFlagService : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(
            FeatureFlag feature, FeatureEvaluationContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated evaluation failure.");

        public ValueTask<bool> IsEnabledAsync(
            FeatureFlag feature, FeatureEvaluationContext? context = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated evaluation failure.");
    }
}
