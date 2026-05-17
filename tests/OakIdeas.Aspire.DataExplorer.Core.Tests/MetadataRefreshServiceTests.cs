using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class MetadataRefreshServiceTests
{
    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenSuccessful_InitiatesAggregationAndReturnsCompleted()
    {
        var expectedMetadata = CreateMetadataRoot("applicationdb", "sql-main");
        var aggregation = new StubMetadataAggregationService(expectedMetadata);
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        var response = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        response.Status.Should().Be(RefreshStatus.Completed);
        response.Metadata.Should().Be(expectedMetadata);
        response.Errors.Should().BeEmpty();
        response.IsPartialSuccess.Should().BeFalse();
        response.CompletedAt.Should().NotBeNull();
        response.StartedAt.Should().BeBefore(response.CompletedAt!.Value);
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenSuccessful_InvalidatesCacheBeforeAggregating()
    {
        var expectedMetadata = CreateMetadataRoot("applicationdb", "sql-main");
        var aggregation = new StubMetadataAggregationService(expectedMetadata);
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        cache.InvalidatedKeys.Should().HaveCount(1);
        cache.InvalidatedKeys[0].ResourceId.Should().Be("sql-main");
        cache.InvalidatedKeys[0].DatabaseName.Should().Be("applicationdb");
        cache.InvalidationCalledBeforeAggregation.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenSuccessful_StoresMetadataInCache()
    {
        var expectedMetadata = CreateMetadataRoot("applicationdb", "sql-main");
        var aggregation = new StubMetadataAggregationService(expectedMetadata);
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        cache.StoredMetadata.Should().ContainKey(("sql-main", "applicationdb"));
        cache.StoredMetadata[("sql-main", "applicationdb")].Should().Be(expectedMetadata);
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenAggregationAlreadyCachesMetadata_DoesNotWriteDuplicateCacheEntry()
    {
        var expectedMetadata = CreateMetadataRoot("applicationdb", "sql-main");
        var cache = new SpyMetadataCache();
        var aggregation = new CachingMetadataAggregationService(expectedMetadata, cache);
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        cache.SetCallCount.Should().Be(1);
        cache.StoredMetadata.Should().ContainKey(("sql-main", "applicationdb"));
        cache.StoredMetadata[("sql-main", "applicationdb")].Should().Be(expectedMetadata);
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenConcurrentRefreshRequested_ReturnsInProgress()
    {
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aggregation = new BlockingMetadataAggregationService(barrier.Task);
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        var firstRefresh = service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);
        await aggregation.AggregationStarted.Task;

        var secondResponse = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        secondResponse.Status.Should().Be(RefreshStatus.InProgress);
        secondResponse.Errors.Should().ContainSingle().Which.Should().Contain("already in progress");

        barrier.SetResult();
        await firstRefresh;
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenCancelled_ReturnsCancelled()
    {
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aggregation = new BlockingMetadataAggregationService(barrier.Task);
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        using var cts = new CancellationTokenSource();
        var refreshTask = service.RefreshDatabaseMetadataAsync(context, cts.Token);
        await aggregation.AggregationStarted.Task;

        await cts.CancelAsync();
        barrier.SetResult();

        var response = await refreshTask;

        response.Status.Should().Be(RefreshStatus.Cancelled);
        response.Errors.Should().ContainSingle().Which.Should().Contain("cancelled");
        response.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenAggregationThrows_ReturnsFailedWithError()
    {
        var aggregation = new FailingMetadataAggregationService("Connection timeout");
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(aggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        var response = await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        response.Status.Should().Be(RefreshStatus.Failed);
        response.Errors.Should().ContainSingle().Which.Should().Contain("provider reported an error");
        response.Error.Should().NotBeNull();
        response.CompletedAt.Should().NotBeNull();
        response.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_AfterFailure_AllowsSubsequentRefresh()
    {
        var failingAggregation = new FailingMetadataAggregationService("error");
        var cache = new SpyMetadataCache();
        using var service = new MetadataRefreshService(failingAggregation, cache, CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        var expectedMetadata = CreateMetadataRoot("applicationdb", "sql-main");
        var successAggregation = new StubMetadataAggregationService(expectedMetadata);
        using var service2 = new MetadataRefreshService(successAggregation, cache, CreateErrorHandler());

        var response = await service2.RefreshDatabaseMetadataAsync(context, CancellationToken.None);

        response.Status.Should().Be(RefreshStatus.Completed);
    }

    [Fact]
    public async Task GetRefreshStatusAsync_BeforeAnyRefresh_ReturnsNull()
    {
        using var service = new MetadataRefreshService(
            new StubMetadataAggregationService(CreateMetadataRoot("db", "res")),
            new SpyMetadataCache(),
            CreateErrorHandler());

        var status = await service.GetRefreshStatusAsync(CancellationToken.None);

        status.Should().BeNull();
    }

    [Fact]
    public async Task GetRefreshStatusAsync_AfterSuccessfulRefresh_ReturnsLastStatus()
    {
        var metadata = CreateMetadataRoot("applicationdb", "sql-main");
        using var service = new MetadataRefreshService(
            new StubMetadataAggregationService(metadata),
            new SpyMetadataCache(),
            CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");

        await service.RefreshDatabaseMetadataAsync(context, CancellationToken.None);
        var status = await service.GetRefreshStatusAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(RefreshStatus.Completed);
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenNullContextProvided_Throws()
    {
        using var service = new MetadataRefreshService(
            new StubMetadataAggregationService(CreateMetadataRoot("db", "res")),
            new SpyMetadataCache(),
            CreateErrorHandler());

        var act = async () => await service.RefreshDatabaseMetadataAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenCancelledBeforeStart_ThrowsOrReturnsCancelled()
    {
        using var service = new MetadataRefreshService(
            new StubMetadataAggregationService(CreateMetadataRoot("db", "res")),
            new SpyMetadataCache(),
            CreateErrorHandler());
        var context = CreateSelectedDatabaseContext("sql-main", "applicationdb");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await service.RefreshDatabaseMetadataAsync(context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static DatabaseMetadataRoot CreateMetadataRoot(string databaseName, string resourceId)
        => new(
            databaseName: databaseName,
            providerType: DatabaseProviderType.SqlServer,
            resourceId: resourceId,
            metadataCollectionTime: DateTimeOffset.UtcNow);

    private static IErrorHandler CreateErrorHandler()
        => new ErrorHandler(NullLogger<ErrorHandler>.Instance, []);

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

    private sealed class StubMetadataAggregationService(DatabaseMetadataRoot metadata) : IMetadataAggregationService
    {
        private readonly DatabaseMetadataRoot _metadata = metadata;

        public Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new DiscoverDatabaseMetadataResponse(_metadata));
        }
    }

    private sealed class FailingMetadataAggregationService(string errorMessage) : IMetadataAggregationService
    {
        private readonly string _errorMessage = errorMessage;

        public Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(_errorMessage);
        }
    }

    private sealed class CachingMetadataAggregationService(
        DatabaseMetadataRoot metadata,
        IMetadataCache metadataCache) : IMetadataAggregationService
    {
        private readonly DatabaseMetadataRoot _metadata = metadata;
        private readonly IMetadataCache _metadataCache = metadataCache;

        public async Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
        {
            await _metadataCache.SetAsync(
                selectedDbContext.Resource.ResourceId,
                selectedDbContext.Resource.DatabaseName,
                _metadata,
                cancellationToken);

            return new DiscoverDatabaseMetadataResponse(_metadata);
        }
    }

    private sealed class BlockingMetadataAggregationService(Task releaseSignal) : IMetadataAggregationService
    {
        private readonly Task _releaseSignal = releaseSignal;

        public TaskCompletionSource AggregationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
        {
            AggregationStarted.TrySetResult();
            await _releaseSignal.WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new DiscoverDatabaseMetadataResponse(new DatabaseMetadataRoot(
                databaseName: selectedDbContext.Resource.DatabaseName,
                providerType: DatabaseProviderType.SqlServer,
                resourceId: selectedDbContext.Resource.ResourceId,
                metadataCollectionTime: DateTimeOffset.UtcNow));
        }
    }

    private sealed class SpyMetadataCache : IMetadataCache
    {
        private readonly Dictionary<(string, string), DatabaseMetadataRoot> _store = [];
        public List<(string ResourceId, string DatabaseName)> InvalidatedKeys { get; } = [];
        public Dictionary<(string, string), DatabaseMetadataRoot> StoredMetadata => _store;
        public bool InvalidationCalledBeforeAggregation { get; private set; }
        public int SetCallCount { get; private set; }
        private bool _setHasBeenCalled;

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
            SetCallCount++;
            _setHasBeenCalled = true;
            _store[(resourceId, databaseName)] = metadata;
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(
            string resourceId,
            string databaseName,
            CancellationToken cancellationToken)
        {
            InvalidatedKeys.Add((resourceId, databaseName));
            _store.Remove((resourceId, databaseName));
            if (!_setHasBeenCalled)
            {
                InvalidationCalledBeforeAggregation = true;
            }

            return Task.CompletedTask;
        }
    }
}
