using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Hosting.Services;

internal sealed class AspireResourceDiscovery : IAspireResourceDiscovery
{
    private const string DevelopmentOnlyMessage = "Aspire resource discovery can only be used in Development environments.";

    private readonly DistributedApplicationModel distributedApplicationModel;
    private readonly IHostEnvironment hostEnvironment;
    private readonly IOptions<DataExplorerOptions> options;
    private readonly DiscoveredDatabaseResourceProjector projector;
    private readonly IErrorHandler errorHandler;

    public AspireResourceDiscovery(
        DistributedApplicationModel distributedApplicationModel,
        IHostEnvironment hostEnvironment,
        IOptions<DataExplorerOptions> options,
        DiscoveredDatabaseResourceProjector projector,
        IErrorHandler errorHandler)
    {
        this.distributedApplicationModel = distributedApplicationModel;
        this.hostEnvironment = hostEnvironment;
        this.options = options;
        this.projector = projector;
        this.errorHandler = errorHandler;
    }

    public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
        DiscoverResourcesRequest request,
        CancellationToken cancellationToken)
    {
        DevelopmentEnvironmentGuard.EnsureDevelopment(hostEnvironment.IsDevelopment(), DevelopmentOnlyMessage);
        cancellationToken.ThrowIfCancellationRequested();

        cancellationToken.ThrowIfCancellationRequested();

        if (!options.Value.EnableAspireResourceDiscovery)
        {
            return Task.FromResult(new DiscoverResourcesResponse([]));
        }

        try
        {
            var descriptors = distributedApplicationModel.Resources
                .OfType<SqlServerDatabaseResource>()
                .Select(CreateDescriptor)
                .ToArray();

            var includeUnavailableResources = request.IncludeUnavailableResources
                ?? options.Value.IncludeUnavailableResources;

            var response = projector.Project(
                descriptors,
                DateTimeOffset.UtcNow,
                includeUnavailableResources);

            return Task.FromResult(response);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw errorHandler.CreateException(ex, new ErrorContext("discover-resources"));
        }
    }

    private static DiscoveredDatabaseResourceDescriptor CreateDescriptor(SqlServerDatabaseResource resource)
    {
        var connectionResource = (IResourceWithConnectionString)resource;
        var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["resourceType"] = nameof(SqlServerDatabaseResource),
            ["serverResourceName"] = resource.Parent.Name,
            ["connectionStringEnvironmentVariable"] = connectionResource.ConnectionStringEnvironmentVariable,
        };

        if (resource.Annotations.OfType<SchemaMigrationsDbContextHint>().LastOrDefault() is { } dbContextHint)
        {
            metadata["schemaMigrationsProjectPath"] = dbContextHint.ProjectPath;
            metadata["schemaMigrationsDbContextType"] = dbContextHint.DbContextTypeName;
        }

        return new DiscoveredDatabaseResourceDescriptor(
            resource.Name,
            resource.Name,
            resource.DatabaseName,
            "sqlserver",
            metadata,
            true);
    }
}
