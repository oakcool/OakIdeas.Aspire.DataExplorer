using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class FeatureFlagCatalogTests
{
    [Fact]
    public void Constructor_WhenFeaturesAreValid_CreatesCatalog()
    {
        var features = new[] { CreateFeature("Test.Feature") };

        var catalog = new FeatureFlagCatalog(features);

        catalog.Features.Should().HaveCount(1);
    }

    [Fact]
    public void Constructor_WhenDuplicateKey_Throws()
    {
        var features = new[]
        {
            CreateFeature("Test.Feature"),
            CreateFeature("Test.Feature"),
        };

        var act = () => new FeatureFlagCatalog(features);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*registered more than once*");
    }

    [Fact]
    public void Constructor_WhenEmptyList_CreatesCatalog()
    {
        var catalog = new FeatureFlagCatalog([]);

        catalog.Features.Should().BeEmpty();
    }

    [Fact]
    public void TryGet_WhenKeyExists_ReturnsFeature()
    {
        var feature = CreateFeature("Query.Editor");
        var catalog = new FeatureFlagCatalog([feature]);

        var result = catalog.TryGet("Query.Editor");

        result.Should().NotBeNull();
        result!.Key.Should().Be("Query.Editor");
    }

    [Fact]
    public void TryGet_WhenKeyNotFound_ReturnsNull()
    {
        var catalog = new FeatureFlagCatalog([CreateFeature("Test.Feature")]);

        var result = catalog.TryGet("Unknown.Feature");

        result.Should().BeNull();
    }

    [Fact]
    public void TryGet_WithOutParam_WhenKeyExists_ReturnsTrueAndFeature()
    {
        var feature = CreateFeature("Test.Feature");
        var catalog = new FeatureFlagCatalog([feature]);

        var found = catalog.TryGet("Test.Feature", out var result);

        found.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public void TryGet_WithOutParam_WhenKeyNotFound_ReturnsFalse()
    {
        var catalog = new FeatureFlagCatalog([CreateFeature("Test.Feature")]);

        var found = catalog.TryGet("Unknown.Feature", out var result);

        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        var catalog = new FeatureFlagCatalog([CreateFeature("Query.Editor")]);

        catalog.TryGet("query.editor").Should().NotBeNull();
        catalog.TryGet("QUERY.EDITOR").Should().NotBeNull();
        catalog.TryGet("Query.Editor").Should().NotBeNull();
    }

    [Fact]
    public void ValidateDependencies_WhenAllDependenciesAreRegistered_ReturnsNoErrors()
    {
        var features = new[]
        {
            CreateFeature("Query.Editor"),
            CreateFeatureWithDeps("Query.AutoExecute", "Query.Editor"),
        };
        var catalog = new FeatureFlagCatalog(features);

        var errors = catalog.ValidateDependencies();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDependencies_WhenDependencyNotRegistered_ReturnsError()
    {
        var features = new[]
        {
            CreateFeatureWithDeps("Query.AutoExecute", "Query.Editor"),
        };
        var catalog = new FeatureFlagCatalog(features);

        var errors = catalog.ValidateDependencies();

        errors.Should().ContainSingle()
            .Which.Should().Contain("Query.Editor");
    }

    [Fact]
    public void ValidateDependencies_WhenCycleExists_ReturnsError()
    {
        var features = new[]
        {
            CreateFeatureWithDeps("Feature.A", "Feature.B"),
            CreateFeatureWithDeps("Feature.B", "Feature.A"),
        };
        var catalog = new FeatureFlagCatalog(features);

        var errors = catalog.ValidateDependencies();

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplicationCatalog_HasNoDuplicateKeys()
    {
        var catalog = new FeatureFlagCatalog(ApplicationFeatures.All);

        catalog.Features.Select(f => f.Key)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ApplicationCatalog_ValidateDependencies_ReturnsNoErrors()
    {
        var catalog = new FeatureFlagCatalog(ApplicationFeatures.All);

        var errors = catalog.ValidateDependencies();

        errors.Should().BeEmpty();
    }

    private static FeatureFlag CreateFeature(string key) => new()
    {
        Key = key,
        DisplayName = key,
        Description = key,
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
    };

    private static FeatureFlag CreateFeatureWithDeps(string key, params string[] deps) => new()
    {
        Key = key,
        DisplayName = key,
        Description = key,
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
        DependsOn = deps,
    };
}
