using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Data.Tests.FeatureFlags;

public sealed class DatabaseFeatureFlagSourceProviderTests
{
    private static readonly FeatureFlag TestFeature = new()
    {
        Key = "Test.Feature",
        DisplayName = "Test Feature",
        Description = "A test feature",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
    };

    [Fact]
    public async Task TryGetAsync_WhenRecordEnabled_ReturnsEnabled()
    {
        var repository = new FakeFeatureFlagRepository();
        repository.Records[TestFeature.Key] = new FeatureFlagRecord(
            TestFeature.Key, IsEnabled: true, Notes: null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, RowVersion: 0);

        var provider = CreateProvider(repository);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.Enabled);
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task TryGetAsync_WhenRecordDisabled_ReturnsDisabled()
    {
        var repository = new FakeFeatureFlagRepository();
        repository.Records[TestFeature.Key] = new FeatureFlagRecord(
            TestFeature.Key, IsEnabled: false, Notes: null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, RowVersion: 0);

        var provider = CreateProvider(repository);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.Disabled);
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task TryGetAsync_WhenKeyNotInDatabase_ReturnsNotDefined()
    {
        var repository = new FakeFeatureFlagRepository();
        var provider = CreateProvider(repository);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.NotDefined);
    }

    [Fact]
    public async Task TryGetAsync_WhenRepositoryThrows_ReturnsSourceUnavailable()
    {
        var repository = new FakeFeatureFlagRepository { ThrowOnGet = true };
        var provider = CreateProvider(repository);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.SourceUnavailable);
        result.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TryGetAsync_WhenInitializationThrows_ReturnsSourceUnavailable()
    {
        var repository = new FakeFeatureFlagRepository { ThrowOnInitialize = true };
        var provider = CreateProvider(repository);

        var result = await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        result.Outcome.Should().Be(FeatureFlagSourceOutcome.SourceUnavailable);
    }

    [Fact]
    public async Task TryGetAsync_InitializesLazilyAndOnlyOnce()
    {
        var repository = new FakeFeatureFlagRepository();
        var provider = CreateProvider(repository);

        repository.InitializeCallCount.Should().Be(0);

        await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);
        await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);
        await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        repository.InitializeCallCount.Should().Be(1);
        repository.SeedCallCount.Should().Be(1);
    }

    [Fact]
    public async Task TryGetAsync_ConcurrentCalls_InitializeOnlyOnce()
    {
        var repository = new FakeFeatureFlagRepository();
        var provider = CreateProvider(repository);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty).AsTask());

        await Task.WhenAll(tasks);

        repository.InitializeCallCount.Should().Be(1);
        repository.SeedCallCount.Should().Be(1);
    }

    [Fact]
    public async Task TryGetAsync_SeedsCatalogFeaturesOnInitialization()
    {
        var repository = new FakeFeatureFlagRepository();
        var catalog = new FeatureFlagCatalog([TestFeature]);
        var provider = new DatabaseFeatureFlagSourceProvider(
            repository, catalog, NullLogger<DatabaseFeatureFlagSourceProvider>.Instance);

        await provider.TryGetAsync(TestFeature, FeatureEvaluationContext.Empty);

        repository.SeededFeatures.Should().ContainSingle(f => f.Key == TestFeature.Key);
    }

    private static DatabaseFeatureFlagSourceProvider CreateProvider(FakeFeatureFlagRepository repository)
    {
        var catalog = new FeatureFlagCatalog([TestFeature]);
        return new DatabaseFeatureFlagSourceProvider(
            repository, catalog, NullLogger<DatabaseFeatureFlagSourceProvider>.Instance);
    }

    private sealed class FakeFeatureFlagRepository : IFeatureFlagRepository
    {
        public Dictionary<string, FeatureFlagRecord> Records { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<FeatureFlag> SeededFeatures { get; } = [];
        public int InitializeCallCount;
        public int SeedCallCount;
        public bool ThrowOnGet { get; set; }
        public bool ThrowOnInitialize { get; set; }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (ThrowOnInitialize)
            {
                throw new InvalidOperationException("Simulated initialization failure.");
            }

            Interlocked.Increment(ref InitializeCallCount);
            return Task.CompletedTask;
        }

        public Task SeedAsync(IEnumerable<FeatureFlag> features, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref SeedCallCount);
            SeededFeatures.AddRange(features);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FeatureFlagRecord>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FeatureFlagRecord>>(Records.Values.ToList());

        public Task<FeatureFlagRecord?> TryGetAsync(string key, CancellationToken cancellationToken)
        {
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("Simulated repository failure.");
            }

            return Task.FromResult(Records.TryGetValue(key, out var record) ? record : null);
        }

        public Task UpsertAsync(string key, bool isEnabled, string? notes, CancellationToken cancellationToken)
        {
            Records[key] = new FeatureFlagRecord(key, isEnabled, notes, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0);
            return Task.CompletedTask;
        }
    }
}
