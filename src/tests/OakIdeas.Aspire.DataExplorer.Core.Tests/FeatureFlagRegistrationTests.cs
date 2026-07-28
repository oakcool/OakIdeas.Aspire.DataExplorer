using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class FeatureFlagRegistrationTests
{
    [Fact]
    public void AddFeatureFlags_RegistersFeatureFlagService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags();

        using var sp = services.BuildServiceProvider();
        var service = sp.GetService<IFeatureFlagService>();

        service.Should().NotBeNull();
    }

    [Fact]
    public void AddFeatureFlags_RegistersFeatureFlagCatalog()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetService<IFeatureFlagCatalog>();

        catalog.Should().NotBeNull();
    }

    [Fact]
    public void AddFeatureFlags_CatalogContainsAllApplicationFeatures()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeatureFlags();

        using var sp = services.BuildServiceProvider();
        var catalog = sp.GetRequiredService<IFeatureFlagCatalog>();

        foreach (var feature in ApplicationFeatures.All)
        {
            catalog.TryGet(feature.Key).Should().NotBeNull(
                $"feature '{feature.Key}' should be registered in the catalog");
        }
    }

    [Fact]
    public void AddConfigurationFeatureFlagSource_RegistersProvider()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureFlags()
            .AddConfigurationFeatureFlagSource();

        using var sp = services.BuildServiceProvider();
        var providers = sp.GetServices<OrderedSourceProvider>().ToArray();

        providers.Should().ContainSingle(
            p => p.Provider is ConfigurationFeatureFlagSourceProvider);
    }

    [Fact]
    public void AddConfigurationFeatureFlagSource_UsesDefaultPriority()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureFlags()
            .AddConfigurationFeatureFlagSource();

        using var sp = services.BuildServiceProvider();
        var providers = sp.GetServices<OrderedSourceProvider>().ToArray();

        providers.Should().ContainSingle(
            p => p.Priority == FeatureFlagServiceCollectionExtensions.ConfigurationSourcePriority);
    }

    [Fact]
    public async Task AddFeatureFlags_ServiceCanEvaluateFeature()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureFlags()
            .AddConfigurationFeatureFlagSource();

        using var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<IFeatureFlagService>();

        var result = await service.IsEnabledAsync(ApplicationFeatures.QueryEditor);

        result.Should().BeTrue("Query.Editor defaults to enabled");
    }

    [Fact]
    public async Task AddFeatureFlags_WhenConfigurationDisablesFeature_ReturnsFalse()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OakIdeas:Aspire:DataExplorer:FeatureFlags:Query.Editor"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureFlags()
            .AddConfigurationFeatureFlagSource();

        using var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<IFeatureFlagService>();

        var result = await service.IsEnabledAsync(ApplicationFeatures.QueryEditor);

        result.Should().BeFalse("configuration overrode the default enabled state");
    }
}
