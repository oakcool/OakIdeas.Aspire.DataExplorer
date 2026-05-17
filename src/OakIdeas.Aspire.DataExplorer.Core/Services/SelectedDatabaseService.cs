using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class SelectedDatabaseService(
    IAspireResourceDiscovery resourceDiscovery,
    IErrorHandler errorHandler) : ISelectedDatabaseService
{
    private readonly IAspireResourceDiscovery _resourceDiscovery = resourceDiscovery;
    private readonly IErrorHandler _errorHandler = errorHandler;
    private SelectedDatabaseContext? selectedDatabase;

    public event EventHandler<SelectedDatabaseContext?>? SelectionChanged;

    public async Task<SelectDatabaseResponse> SelectDatabaseAsync(
        string resourceId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new SelectDatabaseRequest(resourceId);

        if (string.IsNullOrWhiteSpace(request.ResourceId))
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ResourceNotFound,
                "A database resource identifier is required.",
                "Select an available database resource and try again.",
                new ErrorContext("select-database"));
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: error.Message,
                Error: error);
        }

        DiscoverResourcesResponse discoveredResources;
        try
        {
            discoveredResources = await _resourceDiscovery.DiscoverResourcesAsync(
                new DiscoverResourcesRequest(IncludeUnavailableResources: true),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = _errorHandler.MapException(ex, new ErrorContext("select-database", request.ResourceId));
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: error.Message,
                Error: error);
        }

        var resource = discoveredResources.Resources
            .FirstOrDefault(candidate => string.Equals(
                candidate.ResourceId,
                request.ResourceId,
                StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ResourceNotFound,
                $"The database resource '{request.ResourceId}' could not be found.",
                "Refresh discovered resources and select a different database.",
                new ErrorContext("select-database", request.ResourceId));
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: error.Message,
                Error: error);
        }

        if (!resource.IsAvailable)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ConnectionFailed,
                $"The database resource '{request.ResourceId}' is unavailable.",
                "Confirm the database is running, then refresh and try again.",
                new ErrorContext("select-database", request.ResourceId, resource.ProviderType));
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: error.Message,
                Error: error);
        }

        if (resource.ProviderType is DatabaseProviderType.Unknown)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ProviderError,
                $"The database resource '{request.ResourceId}' has an unsupported provider configuration.",
                "Select a supported development database resource and try again.",
                new ErrorContext("select-database", request.ResourceId));
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: error.Message,
                Error: error);
        }

        var nextSelection = new SelectedDatabaseContext(
            Resource: resource,
            IsValid: true,
            ValidationMessage: null);

        if (!Equals(selectedDatabase, nextSelection))
        {
            selectedDatabase = nextSelection;
            SelectionChanged?.Invoke(this, selectedDatabase);
        }

        return new SelectDatabaseResponse(
            Succeeded: true,
            Context: selectedDatabase,
            ErrorMessage: null,
            Error: null);
    }

    public Task<SelectedDatabaseContext?> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(selectedDatabase);
    }

    public Task ClearSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (selectedDatabase is not null)
        {
            selectedDatabase = null;
            SelectionChanged?.Invoke(this, null);
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsSelectedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(selectedDatabase is not null);
    }
}
