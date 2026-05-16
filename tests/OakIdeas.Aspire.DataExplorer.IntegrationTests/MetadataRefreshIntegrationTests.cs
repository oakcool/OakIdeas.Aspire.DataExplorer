using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class MetadataRefreshIntegrationTests
{
    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenSchemaChanges_ReturnsUpdatedMetadata()
    {
        var firstMetadata = new DatabaseMetadataRoot(
            databaseName: "applicationdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-main",
            metadataCollectionTime: DateTimeOffset.UtcNow,
            objects: new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>
            {
                [DatabaseObjectType.Table] = new Dictionary<string, DatabaseObject>
                {
                    ["dbo.Products"] = new TableObject("table.products", "dbo", "Products"),
                },
            });

        var secondMetadata = new DatabaseMetadataRoot(
            databaseName: "applicationdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-main",
            metadataCollectionTime: DateTimeOffset.UtcNow,
            objects: new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>
            {
                [DatabaseObjectType.Table] = new Dictionary<string, DatabaseObject>
                {
                    ["dbo.Products"] = new TableObject("table.products", "dbo", "Products"),
                    ["dbo.Customers"] = new TableObject("table.customers", "dbo", "Customers"),
                },
            });

        var aggregation = new SequentialMetadataAggregationService([firstMetadata, secondMetadata]);
        var cache = new InMemoryMetadataCache();

        using var service = new MetadataRefreshService(aggregation, cache);
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        var firstResponse = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);
        var secondResponse = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        firstResponse.Status.Should().Be(RefreshStatus.Completed);
        firstResponse.Metadata!.Objects[DatabaseObjectType.Table].Should().HaveCount(1);

        secondResponse.Status.Should().Be(RefreshStatus.Completed);
        secondResponse.Metadata!.Objects[DatabaseObjectType.Table].Should().HaveCount(2);
        secondResponse.Metadata.Objects[DatabaseObjectType.Table].Should().ContainKey("dbo.Customers");
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenProviderFails_ReturnsErrorDetails()
    {
        var aggregation = new FailingMetadataAggregationService("Provider unavailable: connection refused");
        var cache = new InMemoryMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache);
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        var response = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        response.Status.Should().Be(RefreshStatus.Failed);
        response.Errors.Should().ContainSingle();
        response.Errors[0].Should().Contain("connection refused");
        response.Metadata.Should().BeNull();
        response.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenRefreshInProgress_RefusesSecondRequest()
    {
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aggregation = new BlockingMetadataAggregationService(barrier.Task);
        var cache = new InMemoryMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache);
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        var firstRefresh = service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);
        await aggregation.AggregationStarted.Task;

        var secondResponse = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        secondResponse.Status.Should().Be(RefreshStatus.InProgress);
        secondResponse.Errors.Should().ContainSingle();

        barrier.SetResult();
        var firstResponse = await firstRefresh;
        firstResponse.Status.Should().Be(RefreshStatus.Completed);
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenRegisteredViaDI_ResolvesAndWorks()
    {
        var expectedMetadata = new DatabaseMetadataRoot(
            databaseName: "applicationdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-main",
            metadataCollectionTime: DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddSingleton<IMetadataAggregationService>(
            new SequentialMetadataAggregationService([expectedMetadata]));
        services.AddSingleton<IMetadataCache, InMemoryMetadataCache>();
        services.AddMetadataRefreshService();

        await using var scope = services.BuildServiceProvider().CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IMetadataRefreshService>();

        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");
        var response = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        response.Status.Should().Be(RefreshStatus.Completed);
        response.Metadata.Should().NotBeNull();
        response.Metadata!.DatabaseName.Should().Be("applicationdb");
    }

    [Fact]
    public async Task GetRefreshStatusAsync_AfterMultipleRefreshes_ReturnsLastRefreshStatus()
    {
        var firstMetadata = new DatabaseMetadataRoot(
            databaseName: "applicationdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-main",
            metadataCollectionTime: DateTimeOffset.UtcNow);

        var secondMetadata = new DatabaseMetadataRoot(
            databaseName: "applicationdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-main",
            metadataCollectionTime: DateTimeOffset.UtcNow);

        var aggregation = new SequentialMetadataAggregationService([firstMetadata, secondMetadata]);
        var cache = new InMemoryMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache);
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);
        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);
        var status = await service.GetRefreshStatusAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(RefreshStatus.Completed);
        status.Metadata.Should().Be(secondMetadata);
    }

    private static SelectedDatabaseContext CreateSelectedDatabaseContext(
        string resourceId,
        string databaseName)
        => new(
            Resource: new DiscoveredDatabaseResource(
                ResourceId: resourceId,
                ResourceName: resourceId,
                DatabaseName: databaseName,
                ProviderType: DatabaseProviderType.SqlServer,
                ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>()),
                IsAvailable: true,
                DiscoveredAt: DateTimeOffset.UtcNow),
            IsValid: true,
            ValidationMessage: null);

    private sealed class SequentialMetadataAggregationService(IReadOnlyList<DatabaseMetadataRoot> sequence)
        : IMetadataAggregationService
    {
        private readonly IReadOnlyList<DatabaseMetadataRoot> _sequence = sequence;
        private int _callCount;

        public Task<DiscoverDatabaseMetadataResponse> DiscoverDatabaseMetadataAsync(
            DiscoverDatabaseMetadataRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = Math.Min(_callCount++, _sequence.Count - 1);
            return Task.FromResult(new DiscoverDatabaseMetadataResponse(_sequence[index]));
        }
    }

    private sealed class FailingMetadataAggregationService(string errorMessage) : IMetadataAggregationService
    {
        private readonly string _errorMessage = errorMessage;

        public Task<DiscoverDatabaseMetadataResponse> DiscoverDatabaseMetadataAsync(
            DiscoverDatabaseMetadataRequest request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(_errorMessage);
        }
    }

    private sealed class BlockingMetadataAggregationService(Task releaseSignal) : IMetadataAggregationService
    {
        private readonly Task _releaseSignal = releaseSignal;

        public TaskCompletionSource AggregationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<DiscoverDatabaseMetadataResponse> DiscoverDatabaseMetadataAsync(
            DiscoverDatabaseMetadataRequest request,
            CancellationToken cancellationToken)
        {
            AggregationStarted.TrySetResult();
            await _releaseSignal.WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new DiscoverDatabaseMetadataResponse(new DatabaseMetadataRoot(
                databaseName: request.DatabaseName,
                providerType: DatabaseProviderType.SqlServer,
                resourceId: request.ResourceId,
                metadataCollectionTime: DateTimeOffset.UtcNow));
        }
    }

    private sealed class InMemoryMetadataCache : IMetadataCache
    {
        private readonly Dictionary<(string, string), DatabaseMetadataRoot> _store = [];

        public Task<DatabaseMetadataRoot?> GetAsync(
            string resourceId,
            string databaseName,
            CancellationToken cancellationToken)
        {
            _store.TryGetValue((resourceId, databaseName), out var result);
            return Task.FromResult(result);
        }

        public Task SetAsync(
            string resourceId,
            string databaseName,
            DatabaseMetadataRoot metadata,
            CancellationToken cancellationToken)
        {
            _store[(resourceId, databaseName)] = metadata;
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(
            string resourceId,
            string databaseName,
            CancellationToken cancellationToken)
        {
            _store.Remove((resourceId, databaseName));
            return Task.CompletedTask;
        }
    }
}
