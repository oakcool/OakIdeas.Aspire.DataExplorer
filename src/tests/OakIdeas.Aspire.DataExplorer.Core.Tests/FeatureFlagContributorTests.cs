using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class FeatureFlagContributorTests
{
    [Fact]
    public void AddFeatureContributor_RegistersContributorAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags()
            .AddFeatureContributor<StubFeatureContributor>();

        using var sp = services.BuildServiceProvider();
        var contributors = sp.GetServices<IFeatureFlagContributor>().ToArray();

        contributors.Should().ContainSingle(c => c is StubFeatureContributor);
    }

    [Fact]
    public void AddFeatureContributor_ContributorFeaturesAppearInCatalog()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags()
            .AddFeatureContributor<StubFeatureContributor>();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();

        catalog.TryGet("Stub.Alpha").Should().NotBeNull("contributor feature should be in the catalog");
        catalog.TryGet("Stub.Beta").Should().NotBeNull("contributor feature should be in the catalog");
    }

    [Fact]
    public void AddFeatureContributor_ApplicationFeaturesAndContributorFeaturesCoexist()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags()
            .AddFeatureContributor<StubFeatureContributor>();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();

        // Core application features should still be present
        catalog.TryGet(FeatureKeys.ExplorerObjectExplorer).Should().NotBeNull();
        catalog.TryGet(FeatureKeys.QueryEditor).Should().NotBeNull();

        // Contributor features should also be present
        catalog.TryGet("Stub.Alpha").Should().NotBeNull();
    }

    [Fact]
    public async Task AddFeatureContributor_ContributorFeaturesCanBeEvaluated()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureFlags()
            .AddConfigurationFeatureFlagSource()
            .AddFeatureContributor<StubFeatureContributor>();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();
        var service = sp.GetRequiredService<IFeatureFlagService>();

        var alphaFeature = catalog.TryGet("Stub.Alpha");
        alphaFeature.Should().NotBeNull();

        var result = await service.IsEnabledAsync(alphaFeature!);

        result.Should().BeTrue("contributor feature defaults to enabled");
    }

    [Fact]
    public async Task AddFeatureContributor_WhenConfigurationDisablesContributorFeature_ReturnsFalse()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OakIdeas:Aspire:DataExplorer:FeatureFlags:Stub.Alpha"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureFlags()
            .AddConfigurationFeatureFlagSource()
            .AddFeatureContributor<StubFeatureContributor>();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();
        var service = sp.GetRequiredService<IFeatureFlagService>();

        var alphaFeature = catalog.TryGet("Stub.Alpha");
        alphaFeature.Should().NotBeNull();

        var result = await service.IsEnabledAsync(alphaFeature!);

        result.Should().BeFalse("configuration override disabled the contributor feature");
    }

    [Fact]
    public void AddFeatureContributor_DuplicateKeyFromContributorIsIgnored()
    {
        // Catalog deduplicates: if a key is already present (e.g. added via options), the contributor
        // should not cause a duplicate-key exception.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags()
            .AddFeatureContributor<DuplicateKeyContributor>();

        using var sp = services.BuildServiceProvider();

        // Should not throw; the duplicate is silently skipped.
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();
        catalog.Features.Should().ContainSingle(f =>
            string.Equals(f.Key, FeatureKeys.QueryEditor, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddFeatureContributor_MultipleContributorsAreAllIncluded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags()
            .AddFeatureContributor<StubFeatureContributor>()
            .AddFeatureContributor<AnotherStubContributor>();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();

        catalog.TryGet("Stub.Alpha").Should().NotBeNull();
        catalog.TryGet("Stub.Beta").Should().NotBeNull();
        catalog.TryGet("Other.Gamma").Should().NotBeNull();
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubFeatureContributor : IFeatureFlagContributor
    {
        public IReadOnlyList<FeatureFlag> GetFeatures() =>
        [
            new FeatureFlag
            {
                Key = "Stub.Alpha",
                DisplayName = "Stub Alpha",
                Description = "Test contributor feature Alpha.",
                Category = FeatureCategory.Provider,
                DefaultEnabled = true,
                Owner = "Stub",
            },
            new FeatureFlag
            {
                Key = "Stub.Beta",
                DisplayName = "Stub Beta",
                Description = "Test contributor feature Beta.",
                Category = FeatureCategory.Provider,
                DefaultEnabled = true,
                Owner = "Stub",
            },
        ];
    }

    private sealed class AnotherStubContributor : IFeatureFlagContributor
    {
        public IReadOnlyList<FeatureFlag> GetFeatures() =>
        [
            new FeatureFlag
            {
                Key = "Other.Gamma",
                DisplayName = "Other Gamma",
                Description = "Test contributor feature Gamma.",
                Category = FeatureCategory.Provider,
                DefaultEnabled = true,
                Owner = "Other",
            },
        ];
    }

    /// <summary>Contributor that attempts to re-register an existing core key.</summary>
    private sealed class DuplicateKeyContributor : IFeatureFlagContributor
    {
        public IReadOnlyList<FeatureFlag> GetFeatures() =>
        [
            new FeatureFlag
            {
                Key = FeatureKeys.QueryEditor,
                DisplayName = "Duplicate",
                Description = "Duplicate key – should be skipped.",
                Category = FeatureCategory.Provider,
                DefaultEnabled = false,
                Owner = "Test",
            },
        ];
    }
}
