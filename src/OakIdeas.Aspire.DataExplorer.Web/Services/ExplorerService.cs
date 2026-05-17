using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using DataExplorerOperationException = OakIdeas.Aspire.DataExplorer.Core.Models.DataExplorerOperationException;
using ErrorContext = OakIdeas.Aspire.DataExplorer.Core.Models.ErrorContext;
using SelectedDatabaseContext = OakIdeas.Aspire.DataExplorer.Core.Models.SelectedDatabaseContext;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

public sealed class ExplorerService(
    IAspireResourceDiscovery resourceDiscovery,
    ISelectedDatabaseService selectedDatabaseService,
    IMetadataAggregationService metadataAggregationService,
    IMetadataRefreshService metadataRefreshService,
    IProviderFactory providerFactory,
    IErrorHandler errorHandler) : IExplorerService
{
    private readonly IAspireResourceDiscovery _resourceDiscovery = resourceDiscovery;
    private readonly ISelectedDatabaseService _selectedDatabaseService = selectedDatabaseService;
    private readonly IMetadataAggregationService _metadataAggregationService = metadataAggregationService;
    private readonly IMetadataRefreshService _metadataRefreshService = metadataRefreshService;
    private readonly IProviderFactory _providerFactory = providerFactory;
    private readonly IErrorHandler _errorHandler = errorHandler;

    public async Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var discovered = await _resourceDiscovery.DiscoverResourcesAsync(
                new DiscoverResourcesRequest(IncludeUnavailableResources: true),
                cancellationToken);

            return new GetAvailableDatabasesResponse(discovered.Resources);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = ResolveError(ex, new ErrorContext("discover-resources"));
            return new GetAvailableDatabasesResponse([], error);
        }
    }

    public async Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return new SelectDatabaseResponse(
                Succeeded: false,
                Selection: null,
                ValidationErrors: ["Resource ID is required."]);
        }

        var response = await _selectedDatabaseService.SelectDatabaseAsync(resourceId.Trim(), cancellationToken);

        return new SelectDatabaseResponse(
            Succeeded: response.Succeeded,
            Selection: response.Context is null ? null : MapSelection(response.Context),
            ValidationErrors: response.ErrorMessage is null ? [] : [response.ErrorMessage],
            Error: response.Error);
    }

    public async Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selected = await _selectedDatabaseService.GetSelectedDatabaseAsync(cancellationToken);
        return new GetSelectedDatabaseResponse(selected is null ? null : MapSelection(selected));
    }

    public async Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selected = await _selectedDatabaseService.GetSelectedDatabaseAsync(cancellationToken);
        if (selected is null)
        {
            return new GetDatabaseMetadataResponse(
                Metadata: null,
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Failed,
                FailureDetails: [],
                Errors: ["Select an available database before loading metadata."],
                Error: _errorHandler.CreateError(
                    ErrorCategory.ResourceNotFound,
                    "Select an available database before loading metadata.",
                    "Choose a database from Object Explorer and try again.",
                    new ErrorContext("load-metadata")));
        }

        if (!selected.IsValid)
        {
            return new GetDatabaseMetadataResponse(
                Metadata: null,
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Failed,
                FailureDetails: [],
                Errors: [selected.ValidationMessage ?? "The selected database is not valid."],
                Error: _errorHandler.CreateError(
                    ErrorCategory.ConnectionFailed,
                    selected.ValidationMessage ?? "The selected database is not valid.",
                    "Select a different database resource and try again.",
                    new ErrorContext("load-metadata", selected.Resource.DatabaseName, selected.Resource.ProviderType)));
        }

        try
        {
            var response = await _metadataAggregationService.GetDatabaseMetadataAsync(selected, cancellationToken);

            return new GetDatabaseMetadataResponse(
                Metadata: response.Metadata,
                AggregatedMetadata: response.AggregatedMetadata,
                CollectionStatus: response.CollectionStatus,
                FailureDetails: response.FailureDetails ?? [],
                Errors: response.Error is null ? [] : [response.Error.Message],
                Error: response.Error);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = ResolveError(ex, new ErrorContext("load-metadata", selected.Resource.DatabaseName, selected.Resource.ProviderType));
            return new GetDatabaseMetadataResponse(
                Metadata: null,
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Failed,
                FailureDetails: [],
                Errors: [error.Message],
                Error: error);
        }
    }

    public async Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selected = await _selectedDatabaseService.GetSelectedDatabaseAsync(cancellationToken);
        if (selected is null)
        {
            var now = DateTimeOffset.UtcNow;
            return new RefreshMetadataResponse(
                Status: RefreshStatus.Failed,
                StartedAt: now,
                CompletedAt: now,
                Errors: ["Select an available database before refreshing metadata."],
                IsPartialSuccess: false,
                Metadata: null,
                Error: _errorHandler.CreateError(
                    ErrorCategory.ResourceNotFound,
                    "Select an available database before refreshing metadata.",
                    "Choose a database from Object Explorer and try again.",
                    new ErrorContext("refresh-metadata")));
        }

        if (!selected.IsValid)
        {
            var now = DateTimeOffset.UtcNow;
            return new RefreshMetadataResponse(
                Status: RefreshStatus.Failed,
                StartedAt: now,
                CompletedAt: now,
                Errors: [selected.ValidationMessage ?? "The selected database is not valid."],
                IsPartialSuccess: false,
                Metadata: null,
                Error: _errorHandler.CreateError(
                    ErrorCategory.ConnectionFailed,
                    selected.ValidationMessage ?? "The selected database is not valid.",
                    "Select a different database resource and try again.",
                    new ErrorContext("refresh-metadata", selected.Resource.DatabaseName, selected.Resource.ProviderType)));
        }

        try
        {
            return await _metadataRefreshService.RefreshDatabaseMetadataAsync(selected, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var now = DateTimeOffset.UtcNow;
            var error = ResolveError(ex, new ErrorContext("refresh-metadata", selected.Resource.DatabaseName, selected.Resource.ProviderType));
            return new RefreshMetadataResponse(
                Status: RefreshStatus.Failed,
                StartedAt: now,
                CompletedAt: now,
                Errors: [error.Message],
                IsPartialSuccess: false,
                Metadata: null,
                Error: error);
        }
    }

    public async Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(
        string objectId,
        DatabaseObjectType objectType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(objectId))
        {
            return new GetObjectDefinitionResponse(
                ObjectId: objectId,
                ObjectType: objectType,
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Object ID is required.",
                Errors: ["Object ID is required."],
                Error: _errorHandler.CreateError(
                    ErrorCategory.ResourceNotFound,
                    "Object ID is required.",
                    "Select an object from Object Explorer and try again.",
                    new ErrorContext("load-definition")));
        }

        if (objectType is DatabaseObjectType.Unknown)
        {
            return new GetObjectDefinitionResponse(
                ObjectId: objectId.Trim(),
                ObjectType: objectType,
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "A supported object type is required.",
                Errors: ["A supported object type is required."],
                Error: _errorHandler.CreateError(
                    ErrorCategory.ProviderError,
                    "A supported object type is required.",
                    "Select a supported database object and try again.",
                    new ErrorContext("load-definition")));
        }

        var selected = await _selectedDatabaseService.GetSelectedDatabaseAsync(cancellationToken);
        if (selected is null)
        {
            return new GetObjectDefinitionResponse(
                ObjectId: objectId.Trim(),
                ObjectType: objectType,
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "No database is selected.",
                Errors: ["Select an available database before requesting object definitions."],
                Error: _errorHandler.CreateError(
                    ErrorCategory.ResourceNotFound,
                    "Select an available database before requesting object definitions.",
                    "Choose a database from Object Explorer and try again.",
                    new ErrorContext("load-definition")));
        }

        if (!selected.IsValid)
        {
            return new GetObjectDefinitionResponse(
                ObjectId: objectId.Trim(),
                ObjectType: objectType,
                Definition: null,
                IsAvailable: false,
                UnavailableReason: selected.ValidationMessage ?? "The selected database is not valid.",
                Errors: [selected.ValidationMessage ?? "The selected database is not valid."],
                Error: _errorHandler.CreateError(
                    ErrorCategory.ConnectionFailed,
                    selected.ValidationMessage ?? "The selected database is not valid.",
                    "Select a different database resource and try again.",
                    new ErrorContext("load-definition", objectId.Trim(), selected.Resource.ProviderType)));
        }

        try
        {
            var provider = _providerFactory.Create(selected.Resource.ProviderType);
            if (provider is not IObjectDefinitionProvider definitionProvider)
            {
                return new GetObjectDefinitionResponse(
                    ObjectId: objectId.Trim(),
                    ObjectType: objectType,
                    Definition: null,
                    IsAvailable: false,
                    UnavailableReason: $"The provider '{selected.Resource.ProviderType}' does not support object definitions.",
                    Errors: [],
                    Error: _errorHandler.CreateError(
                        ErrorCategory.ProviderError,
                        "The selected provider does not support object definitions.",
                        "Choose a different object or database resource and try again.",
                        new ErrorContext("load-definition", objectId.Trim(), selected.Resource.ProviderType)));
            }

            var request = new ObjectDefinitionRequest(
                ObjectId: objectId.Trim(),
                ObjectType: objectType);

            var definition = await definitionProvider.GetDefinitionAsync(
                CreateDatabaseResource(selected.Resource),
                request,
                cancellationToken);

            return new GetObjectDefinitionResponse(
                ObjectId: objectId.Trim(),
                ObjectType: objectType,
                Definition: definition.Definition,
                IsAvailable: definition.IsAvailable,
                UnavailableReason: definition.UnavailableReason,
                Errors: []);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = ResolveError(ex, new ErrorContext("load-definition", objectId.Trim(), selected.Resource.ProviderType));
            return new GetObjectDefinitionResponse(
                ObjectId: objectId.Trim(),
                ObjectType: objectType,
                Definition: null,
                IsAvailable: false,
                UnavailableReason: error.Message,
                Errors: [error.Message],
                Error: error);
        }
    }

    private DataExplorerError ResolveError(Exception exception, ErrorContext context)
        => exception is DataExplorerOperationException dataExplorerException
            ? dataExplorerException.Error
            : _errorHandler.MapException(exception, context);

    private static ExplorerDatabaseSelection MapSelection(SelectedDatabaseContext context)
        => new(
            ResourceId: context.Resource.ResourceId,
            ResourceName: context.Resource.ResourceName,
            DatabaseName: context.Resource.DatabaseName,
            ProviderType: context.Resource.ProviderType,
            IsAvailable: context.Resource.IsAvailable,
            IsValid: context.IsValid,
            ValidationMessage: context.ValidationMessage);

    private static OakIdeas.Aspire.DataExplorer.Core.Models.DatabaseResource CreateDatabaseResource(DiscoveredDatabaseResource resource)
        => new(
            Name: resource.ResourceName,
            Provider: resource.ProviderType.ToString(),
            ConnectionString: resource.ConnectionMetadata.Properties.TryGetValue("connectionString", out var connectionString)
                ? connectionString ?? string.Empty
                : string.Empty,
            IsLocal: true,
            IsWritable: true);
}
