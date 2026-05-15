using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class SelectedDatabaseService(
    IAspireResourceDiscovery resourceDiscovery) : ISelectedDatabaseService
{
    private readonly IAspireResourceDiscovery resourceDiscovery = resourceDiscovery;
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
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: "Resource ID is required.");
        }

        var discoveredResources = await resourceDiscovery.DiscoverResourcesAsync(
            new DiscoverResourcesRequest(IncludeUnavailableResources: true),
            cancellationToken);

        var resource = discoveredResources.Resources
            .FirstOrDefault(candidate => string.Equals(
                candidate.ResourceId,
                request.ResourceId,
                StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: $"Database resource '{request.ResourceId}' was not found in discovered resources.");
        }

        if (!resource.IsAvailable)
        {
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: $"Database resource '{request.ResourceId}' is unavailable.");
        }

        if (resource.ProviderType is DatabaseProviderType.Unknown)
        {
            return new SelectDatabaseResponse(
                Succeeded: false,
                Context: selectedDatabase,
                ErrorMessage: $"Database resource '{request.ResourceId}' has an unsupported provider configuration.");
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
            ErrorMessage: null);
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
