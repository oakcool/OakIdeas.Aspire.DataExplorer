using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class MetadataRefreshService(
    IMetadataAggregationService aggregationService,
    IMetadataCache metadataCache,
    IErrorHandler errorHandler) : IMetadataRefreshService, IDisposable
{
    private readonly IMetadataAggregationService _aggregationService = aggregationService;
    private readonly IMetadataCache _metadataCache = metadataCache;
    private readonly IErrorHandler _errorHandler = errorHandler;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private RefreshMetadataResponse? _lastRefreshStatus;
    private bool _disposed;

    public async Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(
        SelectedDatabaseContext selectedDbContext,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(selectedDbContext);
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = DateTimeOffset.UtcNow;

        var acquired = await _semaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            // Non-blocking check: if the semaphore is held, a refresh is already in progress.
            // Return immediately to prevent concurrent refreshes.
            return new RefreshMetadataResponse(
                Status: RefreshStatus.InProgress,
                StartedAt: _lastRefreshStatus?.StartedAt ?? startedAt,
                CompletedAt: null,
                Errors: ["A refresh operation is already in progress."],
                IsPartialSuccess: false,
                Metadata: null);
        }

        var inProgressResponse = new RefreshMetadataResponse(
            Status: RefreshStatus.InProgress,
            StartedAt: startedAt,
            CompletedAt: null,
            Errors: [],
            IsPartialSuccess: false,
            Metadata: null);

        _lastRefreshStatus = inProgressResponse;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _metadataCache.InvalidateAsync(
                selectedDbContext.Resource.ResourceId,
                selectedDbContext.Resource.DatabaseName,
                cancellationToken);

            var aggregationResponse = await _aggregationService.GetDatabaseMetadataAsync(
                selectedDbContext,
                cancellationToken);

            var cachedMetadata = await _metadataCache.GetAsync(
                selectedDbContext.Resource.ResourceId,
                selectedDbContext.Resource.DatabaseName,
                cancellationToken);

            if (cachedMetadata is null)
            {
                await _metadataCache.SetAsync(
                    selectedDbContext.Resource.ResourceId,
                    selectedDbContext.Resource.DatabaseName,
                    aggregationResponse.Metadata,
                    cancellationToken);
            }

            var completedResponse = new RefreshMetadataResponse(
                Status: RefreshStatus.Completed,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow,
                Errors: [],
                IsPartialSuccess: false,
                Metadata: aggregationResponse.Metadata,
                Error: aggregationResponse.Error);

            _lastRefreshStatus = completedResponse;
            return completedResponse;
        }
        catch (OperationCanceledException)
        {
            var cancelledResponse = new RefreshMetadataResponse(
                Status: RefreshStatus.Cancelled,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow,
                Errors: ["Refresh was cancelled."],
                IsPartialSuccess: false,
                Metadata: null);

            _lastRefreshStatus = cancelledResponse;
            return cancelledResponse;
        }
        catch (Exception ex)
        {
            var error = _errorHandler.MapException(
                ex,
                new ErrorContext(
                    "refresh-metadata",
                    selectedDbContext.Resource.DatabaseName,
                    selectedDbContext.Resource.ProviderType));
            var failedResponse = new RefreshMetadataResponse(
                Status: RefreshStatus.Failed,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow,
                Errors: [error.Message],
                IsPartialSuccess: false,
                Metadata: null,
                Error: error);

            _lastRefreshStatus = failedResponse;
            return failedResponse;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<RefreshMetadataResponse?> GetRefreshStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_lastRefreshStatus);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Dispose();
        _disposed = true;
    }
}
