using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Implements the <see cref="IRelationshipNavigatorService"/> by delegating to the registered
/// <see cref="IRelationshipNavigationProvider"/> for the currently selected database.
/// Enforces the <c>Navigator.RelationshipAwareNavigator</c> feature flag at every entry point.
/// </summary>
public sealed class RelationshipNavigatorService(
    ISelectedDatabaseService selectedDatabaseService,
    IEnumerable<IRelationshipNavigationProvider> navigationProviders,
    IErrorHandler errorHandler,
    IFeatureFlagService featureFlagService) : IRelationshipNavigatorService
{
    private readonly ISelectedDatabaseService _selectedDatabaseService = selectedDatabaseService;
    private readonly IReadOnlyList<IRelationshipNavigationProvider> _navigationProviders = navigationProviders.ToArray();
    private readonly IErrorHandler _errorHandler = errorHandler;
    private readonly IFeatureFlagService _featureFlagService = featureFlagService;

    /// <inheritdoc />
    public async Task<DiscoverTableRelationshipsResponse> DiscoverRelationshipsAsync(
        DiscoverTableRelationshipsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await IsFeatureDisabledAsync(cancellationToken))
        {
            return new DiscoverTableRelationshipsResponse([]);
        }

        var (resource, provider) = await ResolveAsync(cancellationToken);
        if (resource is null || provider is null)
        {
            return new DiscoverTableRelationshipsResponse([]);
        }

        try
        {
            return await provider.DiscoverTableRelationshipsAsync(resource, request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _errorHandler.MapException(ex, new ErrorContext("navigator-discover-relationships"));
            return new DiscoverTableRelationshipsResponse([]);
        }
    }

    /// <inheritdoc />
    public async Task<GetRelatedRecordCountResponse> GetRelatedRecordCountAsync(
        GetRelatedRecordCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await IsFeatureDisabledAsync(cancellationToken))
        {
            return new GetRelatedRecordCountResponse(0);
        }

        var (resource, provider) = await ResolveAsync(cancellationToken);
        if (resource is null || provider is null)
        {
            return new GetRelatedRecordCountResponse(0);
        }

        try
        {
            return await provider.GetRelatedRecordCountAsync(resource, request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _errorHandler.MapException(ex, new ErrorContext("navigator-record-count"));
            return new GetRelatedRecordCountResponse(0);
        }
    }

    /// <inheritdoc />
    public async Task<NavigateRelatedRecordsResponse> NavigateRelatedRecordsAsync(
        NavigateRelatedRecordsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await IsFeatureDisabledAsync(cancellationToken))
        {
            return new NavigateRelatedRecordsResponse([], 0, false, string.Empty);
        }

        var (resource, provider) = await ResolveAsync(cancellationToken);
        if (resource is null || provider is null)
        {
            return new NavigateRelatedRecordsResponse([], 0, false, string.Empty);
        }

        try
        {
            return await provider.NavigateRelatedRecordsAsync(resource, request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _errorHandler.MapException(ex, new ErrorContext("navigator-navigate-records"));
            return new NavigateRelatedRecordsResponse([], 0, false, string.Empty);
        }
    }

    private async Task<bool> IsFeatureDisabledAsync(CancellationToken cancellationToken)
    {
        var enabled = await _featureFlagService.IsEnabledAsync(
            ApplicationFeatures.RelationshipAwareNavigator,
            null,
            cancellationToken);
        return !enabled;
    }

    private async Task<(DatabaseResource? resource, IRelationshipNavigationProvider? provider)> ResolveAsync(
        CancellationToken cancellationToken)
    {
        var selected = await _selectedDatabaseService.GetSelectedDatabaseAsync(cancellationToken);
        if (selected is null || !selected.IsValid)
        {
            return (null, null);
        }

        var provider = _navigationProviders.FirstOrDefault(p =>
            p.ProviderType == selected.Resource.ProviderType);

        if (provider is null)
        {
            return (null, null);
        }

        var resource = CreateResource(selected);
        return (resource, provider);
    }

    private static DatabaseResource CreateResource(SelectedDatabaseContext context)
    {
        var connectionString = ResolveConnectionString(context.Resource.ConnectionMetadata.Properties);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string could be resolved for resource '{context.Resource.ResourceId}'.");
        }

        return new DatabaseResource(
            Name: context.Resource.ResourceName,
            Provider: context.Resource.ProviderType.ToString(),
            ConnectionString: connectionString,
            IsLocal: true,
            IsWritable: true);
    }

    private static string? ResolveConnectionString(IReadOnlyDictionary<string, string?> metadata)
    {
        if (metadata.TryGetValue("connectionString", out var directConnectionString)
            && !string.IsNullOrWhiteSpace(directConnectionString))
        {
            return directConnectionString;
        }

        if (metadata.TryGetValue("connectionStringEnvironmentVariable", out var environmentVariableName)
            && !string.IsNullOrWhiteSpace(environmentVariableName))
        {
            return Environment.GetEnvironmentVariable(environmentVariableName);
        }

        return null;
    }
}
