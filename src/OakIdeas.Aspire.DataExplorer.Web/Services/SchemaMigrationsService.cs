using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using DataExplorerOperationException = OakIdeas.Aspire.DataExplorer.Core.Models.DataExplorerOperationException;
using ErrorContext = OakIdeas.Aspire.DataExplorer.Core.Models.ErrorContext;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

public sealed class SchemaMigrationsService(
    IAspireResourceDiscovery resourceDiscovery,
    ISelectedDatabaseService selectedDatabaseService,
    IMetadataAggregationService metadataAggregationService,
    IEnumerable<ISchemaMigrationsProvider> schemaMigrationsProviders,
    IErrorHandler errorHandler,
    IFeatureFlagService featureFlagService) : ISchemaMigrationsService
{
    private readonly IAspireResourceDiscovery _resourceDiscovery = resourceDiscovery;
    private readonly ISelectedDatabaseService _selectedDatabaseService = selectedDatabaseService;
    private readonly IMetadataAggregationService _metadataAggregationService = metadataAggregationService;
    private readonly IReadOnlyList<ISchemaMigrationsProvider> _schemaMigrationsProviders = schemaMigrationsProviders.ToArray();
    private readonly IErrorHandler _errorHandler = errorHandler;
    private readonly IFeatureFlagService _featureFlagService = featureFlagService;

    public async Task<SchemaMigrationsOverviewResponse> GetOverviewAsync(
        SchemaMigrationsOverviewRequest request,
        CancellationToken cancellationToken)
    {
        if (await IsFeatureDisabledAsync(cancellationToken) is { } featureDisabled)
        {
            return featureDisabled;
        }

        var selected = await GetSelectedDatabaseAsync(cancellationToken);
        if (selected.response is not null)
        {
            return selected.response;
        }

        var provider = GetProvider(selected.context!);
        if (provider.response is not null)
        {
            return provider.response;
        }

        try
        {
            var liveMetadata = await _metadataAggregationService.GetDatabaseMetadataAsync(selected.context!, cancellationToken);
            if (liveMetadata.AggregatedMetadata is null)
            {
                throw new InvalidOperationException("Database metadata is not available for the selected database.");
            }

            DatabaseMetadata? comparisonMetadata = null;
            string? comparisonDatabaseName = null;

            if (!string.IsNullOrWhiteSpace(request.ComparisonResourceId))
            {
                var comparisonContext = await GetComparisonContextAsync(request.ComparisonResourceId, cancellationToken);
                if (comparisonContext is not null)
                {
                    comparisonDatabaseName = comparisonContext.Resource.DatabaseName;
                    comparisonMetadata = (await _metadataAggregationService.GetDatabaseMetadataAsync(comparisonContext, cancellationToken)).AggregatedMetadata;
                }
            }

            var resource = CreateResource(selected.context!);
            return await provider.provider!.GetOverviewAsync(
                resource,
                selected.context!.Resource.ConnectionMetadata,
                liveMetadata.AggregatedMetadata,
                comparisonMetadata,
                comparisonDatabaseName,
                cancellationToken);
        }
        catch (DataExplorerOperationException ex)
        {
            return new SchemaMigrationsOverviewResponse(
                selected.context!.Resource.DatabaseName,
                null,
                [],
                [],
                [ex.Error.Message],
                null,
                false,
                false,
                false,
                ex.Error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = _errorHandler.MapException(
                ex,
                new ErrorContext("schema-migrations-overview", selected.context!.Resource.DatabaseName, selected.context.Resource.ProviderType));
            return new SchemaMigrationsOverviewResponse(
                selected.context!.Resource.DatabaseName,
                null,
                [],
                [],
                [error.Message],
                null,
                false,
                false,
                false,
                error);
        }
    }

    public async Task<GenerateSchemaMigrationsScriptResponse> GenerateScriptAsync(
        GenerateSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken)
    {
        if (await IsFeatureDisabledAsync(cancellationToken) is { } featureDisabled)
        {
            return new GenerateSchemaMigrationsScriptResponse(
                DatabaseName: string.Empty,
                Script: string.Empty,
                request.Kind,
                IsIdempotent: false,
                Warnings: [featureDisabled.Error!.Message],
                Error: featureDisabled.Error);
        }

        var selected = await GetSelectedDatabaseAsync(cancellationToken);
        if (selected.response is not null)
        {
            return new GenerateSchemaMigrationsScriptResponse(string.Empty, string.Empty, request.Kind, false, selected.response.Warnings, selected.response.Error);
        }

        var provider = GetProvider(selected.context!);
        if (provider.response is not null)
        {
            return new GenerateSchemaMigrationsScriptResponse(string.Empty, string.Empty, request.Kind, false, provider.response.Warnings, provider.response.Error);
        }

        var resource = CreateResource(selected.context!);
        return await provider.provider!.GenerateScriptAsync(resource, selected.context!.Resource.ConnectionMetadata, request, cancellationToken);
    }

    public async Task<ExecuteSchemaMigrationsScriptResponse> ExecuteScriptAsync(
        ExecuteSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken)
    {
        if (await IsFeatureDisabledAsync(cancellationToken) is { } featureDisabled)
        {
            return new ExecuteSchemaMigrationsScriptResponse(string.Empty, false, 0, [featureDisabled.Error!.Message], DateTimeOffset.UtcNow, featureDisabled.Error);
        }

        var selected = await GetSelectedDatabaseAsync(cancellationToken);
        if (selected.response is not null)
        {
            return new ExecuteSchemaMigrationsScriptResponse(string.Empty, false, 0, selected.response.Warnings, DateTimeOffset.UtcNow, selected.response.Error);
        }

        var provider = GetProvider(selected.context!);
        if (provider.response is not null)
        {
            return new ExecuteSchemaMigrationsScriptResponse(string.Empty, false, 0, provider.response.Warnings, DateTimeOffset.UtcNow, provider.response.Error);
        }

        var resource = CreateResource(selected.context!);
        return await provider.provider!.ExecuteScriptAsync(resource, request, cancellationToken);
    }

    private async Task<SchemaMigrationsOverviewResponse?> IsFeatureDisabledAsync(CancellationToken cancellationToken)
    {
        var enabled = await _featureFlagService.IsEnabledAsync(ApplicationFeatures.SchemaMigrations, null, cancellationToken);
        if (enabled)
        {
            return null;
        }

        var error = _errorHandler.CreateError(
            ErrorCategory.FeatureDisabled,
            "The Schema and Migrations feature is disabled by configuration.",
            "Enable Explorer.SchemaMigrations and try again.",
            new ErrorContext("schema-migrations"));
        return new SchemaMigrationsOverviewResponse(string.Empty, null, [], [], [error.Message], null, false, false, false, error);
    }

    private async Task<(SelectedDatabaseContext? context, SchemaMigrationsOverviewResponse? response)> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
    {
        var selected = await _selectedDatabaseService.GetSelectedDatabaseAsync(cancellationToken);
        if (selected is null)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ResourceNotFound,
                "Select an available database before loading schema and migration details.",
                "Choose a database from Object Explorer and try again.",
                new ErrorContext("schema-migrations"));
            return (null, new SchemaMigrationsOverviewResponse(string.Empty, null, [], [], [error.Message], null, false, false, false, error));
        }

        if (!selected.IsValid)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ConnectionFailed,
                selected.ValidationMessage ?? "The selected database is not valid.",
                "Select a different database resource and try again.",
                new ErrorContext("schema-migrations", selected.Resource.DatabaseName, selected.Resource.ProviderType));
            return (null, new SchemaMigrationsOverviewResponse(selected.Resource.DatabaseName, null, [], [], [error.Message], null, false, false, false, error));
        }

        return (selected, null);
    }

    private (ISchemaMigrationsProvider? provider, SchemaMigrationsOverviewResponse? response) GetProvider(SelectedDatabaseContext selectedDatabaseContext)
    {
        var provider = _schemaMigrationsProviders.FirstOrDefault(candidate =>
            candidate.ProviderType == selectedDatabaseContext.Resource.ProviderType);

        if (provider is null)
        {
            var error = _errorHandler.CreateError(
                ErrorCategory.ProviderError,
                "The selected provider does not support schema and migration operations.",
                "Select a different database or enable a provider that supports this feature.",
                new ErrorContext("schema-migrations", selectedDatabaseContext.Resource.DatabaseName, selectedDatabaseContext.Resource.ProviderType));
            return (null, new SchemaMigrationsOverviewResponse(selectedDatabaseContext.Resource.DatabaseName, null, [], [], [error.Message], null, false, false, false, error));
        }

        return (provider, null);
    }

    private async Task<SelectedDatabaseContext?> GetComparisonContextAsync(string resourceId, CancellationToken cancellationToken)
    {
        var discovered = await _resourceDiscovery.DiscoverResourcesAsync(
            new DiscoverResourcesRequest(IncludeUnavailableResources: true),
            cancellationToken);
        var resource = discovered.Resources.FirstOrDefault(candidate =>
            string.Equals(candidate.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase));

        return resource is null || !resource.IsAvailable || resource.ProviderType is DatabaseProviderType.Unknown
            ? null
            : new SelectedDatabaseContext(resource, true, null);
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
