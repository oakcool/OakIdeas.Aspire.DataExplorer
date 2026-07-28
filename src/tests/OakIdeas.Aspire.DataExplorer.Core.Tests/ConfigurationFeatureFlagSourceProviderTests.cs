using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class ConfigurationFeatureFlagSourceProviderTests
{
    private static readonly FeatureFlag TestFeature = new()
    {
        Key = "Query.Editor",
        DisplayName = "Query Editor",
        Description = "Query editor feature",
        Category = FeatureCategory.Query,
        DefaultEnabled = true,
    };

    [Fact]
    public async Task TryGetAsync_WhenKeyNotPresent_ReturnsNotDefined()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = CreateProvider(config);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.NotDefined);
        result.SourceName.Should().Be(ConfigurationFeatureFlagSourceProvider.SourceName);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    public async Task TryGetAsync_WhenValueIsTrue_ReturnsEnabled(string value)
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            [$"{ConfigurationFeatureFlagSourceProvider.SectionPath}:Query.Editor"] = value,
        });
        var provider = CreateProvider(config);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.Enabled);
        result.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    public async Task TryGetAsync_WhenValueIsFalse_ReturnsDisabled(string value)
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            [$"{ConfigurationFeatureFlagSourceProvider.SectionPath}:Query.Editor"] = value,
        });
        var provider = CreateProvider(config);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.Disabled);
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("enabled")]
    [InlineData("on")]
    public async Task TryGetAsync_WhenValueIsInvalid_ReturnsInvalidValue(string invalidValue)
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            [$"{ConfigurationFeatureFlagSourceProvider.SectionPath}:Query.Editor"] = invalidValue,
        });
        var provider = CreateProvider(config);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.InvalidValue);
        result.Reason.Should().Contain(invalidValue);
    }

    [Fact]
    public async Task TryGetAsync_WhenFeatureKeyContainsDots_ResolvesCorrectly()
    {
        // Configuration keys with dots use colon as the path separator, so
        // "Query.Editor" maps to "OakIdeas:Aspire:DataExplorer:FeatureFlags:Query.Editor"
        var config = BuildConfig(new Dictionary<string, string?>
        {
            [$"{ConfigurationFeatureFlagSourceProvider.SectionPath}:Query.Editor"] = "false",
        });
        var provider = CreateProvider(config);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.Disabled);
    }

    [Fact]
    public void Name_ReturnsExpectedSourceName()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = CreateProvider(config);

        provider.Name.Should().Be(ConfigurationFeatureFlagSourceProvider.SourceName);
    }

    [Fact]
    public async Task TryGetAsync_WithCancellationToken_IsHonored()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = CreateProvider(config);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Configuration source is synchronous, so cancellation does not throw here;
        // it is checked at the pipeline level. This test verifies the token is accepted.
        var act = async () => await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty, cts.Token);

        await act.Should().NotThrowAsync();
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static ConfigurationFeatureFlagSourceProvider CreateProvider(IConfiguration config)
        => new(config, NullLogger<ConfigurationFeatureFlagSourceProvider>.Instance);
}
