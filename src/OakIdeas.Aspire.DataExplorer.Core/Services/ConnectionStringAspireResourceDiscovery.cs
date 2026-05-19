using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

internal sealed class ConnectionStringAspireResourceDiscovery : IAspireResourceDiscovery
{
    private const string DevelopmentOnlyMessage = "Aspire resource discovery can only be used in Development environments.";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptions<DataExplorerOptions> _options;
    private readonly DiscoveredDatabaseResourceProjector _projector;
    private readonly IErrorHandler _errorHandler;

    public ConnectionStringAspireResourceDiscovery(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IOptions<DataExplorerOptions> options,
        DiscoveredDatabaseResourceProjector projector,
        IErrorHandler errorHandler)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _options = options;
        _projector = projector;
        _errorHandler = errorHandler;
    }

    public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
        DiscoverResourcesRequest request,
        CancellationToken cancellationToken)
    {
        DevelopmentEnvironmentGuard.EnsureDevelopment(_hostEnvironment.IsDevelopment(), DevelopmentOnlyMessage);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Value.EnableAspireResourceDiscovery)
        {
            return Task.FromResult(new DiscoverResourcesResponse([]));
        }

        try
        {
            var descriptors = _configuration.GetSection("ConnectionStrings")
                .GetChildren()
                .Select(CreateDescriptor)
                .Where(static descriptor => descriptor is not null)
                .Cast<DiscoveredDatabaseResourceDescriptor>()
                .ToArray();

            var includeUnavailableResources = request.IncludeUnavailableResources
                ?? _options.Value.IncludeUnavailableResources;

            return Task.FromResult(_projector.Project(descriptors, DateTimeOffset.UtcNow, includeUnavailableResources));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw _errorHandler.CreateException(ex, new ErrorContext("discover-resources"));
        }
    }

    private static DiscoveredDatabaseResourceDescriptor? CreateDescriptor(IConfigurationSection section)
    {
        if (string.IsNullOrWhiteSpace(section.Key) || string.IsNullOrWhiteSpace(section.Value))
        {
            return null;
        }

        var key = section.Key.Trim();
        var connectionString = section.Value;
        var databaseName = TryGetDatabaseName(connectionString) ?? key;
        var providerHint = InferProviderHint(connectionString);

        var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["resourceType"] = "ConnectionString",
            ["connectionStringEnvironmentVariable"] = $"ConnectionStrings__{key}",
        };

        return new DiscoveredDatabaseResourceDescriptor(
            ResourceId: key,
            ResourceName: key,
            DatabaseName: databaseName,
            ProviderHint: providerHint,
            ConnectionMetadata: metadata,
            IsAvailable: true);
    }

    private static string? TryGetDatabaseName(string connectionString)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            if (TryGetValue(builder, "Database", out var database))
            {
                return database;
            }

            if (TryGetValue(builder, "Initial Catalog", out var initialCatalog))
            {
                return initialCatalog;
            }
        }
        catch (ArgumentException)
        {
        }

        return null;
    }

    private static bool TryGetValue(DbConnectionStringBuilder builder, string key, out string? value)
    {
        if (builder.TryGetValue(key, out var rawValue))
        {
            value = rawValue?.ToString()?.Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static string InferProviderHint(string connectionString)
    {
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return "postgresql";
        }

        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            && connectionString.Contains("Version=", StringComparison.OrdinalIgnoreCase)
            && connectionString.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return "sqlite";
        }

        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return "sqlserver";
        }

        return "unknown";
    }
}