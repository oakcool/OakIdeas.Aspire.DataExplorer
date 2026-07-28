using FluentAssertions;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Data.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Data.Infrastructure.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Data.Tests.FeatureFlags;

public sealed class SqliteFeatureFlagRepositoryTests
{
    private static readonly FeatureFlag FeatureA = new()
    {
        Key = "Test.FeatureA",
        DisplayName = "Feature A",
        Description = "A test feature",
        Category = FeatureCategory.Explorer,
        DefaultEnabled = true,
    };

    private static readonly FeatureFlag FeatureB = new()
    {
        Key = "Test.FeatureB",
        DisplayName = "Feature B",
        Description = "Another test feature",
        Category = FeatureCategory.Query,
        DefaultEnabled = false,
    };

    [Fact]
    public async Task InitializeAsync_CreatesSchema_AndIsIdempotent()
    {
        var repository = CreateRepository();

        await repository.InitializeAsync(CancellationToken.None);
        await repository.InitializeAsync(CancellationToken.None);

        var all = await repository.GetAllAsync(CancellationToken.None);
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_OnlyInsertsOnce()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        await repository.SeedAsync([FeatureA, FeatureB], CancellationToken.None);
        await repository.SeedAsync([FeatureA, FeatureB], CancellationToken.None);

        var all = await repository.GetAllAsync(CancellationToken.None);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task SeedAsync_UsesCatalogDefaultEnabledValue()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        await repository.SeedAsync([FeatureA, FeatureB], CancellationToken.None);

        var recordA = await repository.TryGetAsync(FeatureA.Key, CancellationToken.None);
        var recordB = await repository.TryGetAsync(FeatureB.Key, CancellationToken.None);

        recordA!.IsEnabled.Should().BeTrue();
        recordB!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SeedAsync_AfterManualOverride_DoesNotOverwriteExistingValue()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        await repository.UpsertAsync(FeatureA.Key, isEnabled: false, notes: "manually disabled", CancellationToken.None);
        await repository.SeedAsync([FeatureA], CancellationToken.None);

        var record = await repository.TryGetAsync(FeatureA.Key, CancellationToken.None);

        record!.IsEnabled.Should().BeFalse();
        record.Notes.Should().Be("manually disabled");
    }

    [Fact]
    public async Task TryGetAsync_WhenKeyMissing_ReturnsNull()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        var record = await repository.TryGetAsync("Nonexistent.Key", CancellationToken.None);

        record.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_WhenKeyMissing_CreatesRecord()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        await repository.UpsertAsync(FeatureA.Key, isEnabled: true, notes: "created", CancellationToken.None);

        var record = await repository.TryGetAsync(FeatureA.Key, CancellationToken.None);
        record.Should().NotBeNull();
        record!.IsEnabled.Should().BeTrue();
        record.Notes.Should().Be("created");
        record.RowVersion.Should().Be(0);
    }

    [Fact]
    public async Task UpsertAsync_WhenKeyExists_UpdatesRecordAndIncrementsRowVersion()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        await repository.UpsertAsync(FeatureA.Key, isEnabled: true, notes: "initial", CancellationToken.None);
        await repository.UpsertAsync(FeatureA.Key, isEnabled: false, notes: "updated", CancellationToken.None);

        var record = await repository.TryGetAsync(FeatureA.Key, CancellationToken.None);
        record.Should().NotBeNull();
        record!.IsEnabled.Should().BeFalse();
        record.Notes.Should().Be("updated");
        record.RowVersion.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRecordsOrderedByKey()
    {
        var repository = CreateRepository();
        await repository.InitializeAsync(CancellationToken.None);

        await repository.SeedAsync([FeatureB, FeatureA], CancellationToken.None);

        var all = await repository.GetAllAsync(CancellationToken.None);

        all.Should().HaveCount(2);
        all.Select(r => r.Key).Should().BeInAscendingOrder();
    }

    private static SqliteFeatureFlagRepository CreateRepository()
    {
        // The repository keeps a single open connection for its lifetime, so a plain
        // ":memory:" connection string is safe here and does not require shared cache mode.
        var options = Options.Create(new SqliteFeatureFlagOptions
        {
            ConnectionString = "Data Source=:memory:",
        });
        return new SqliteFeatureFlagRepository(options);
    }
}
