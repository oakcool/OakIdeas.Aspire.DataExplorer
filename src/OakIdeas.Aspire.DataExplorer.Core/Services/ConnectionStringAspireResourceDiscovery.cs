using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ConnectionStringAspireResourceDiscovery> _logger;

    public ConnectionStringAspireResourceDiscovery(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IOptions<DataExplorerOptions> options,
        DiscoveredDatabaseResourceProjector projector,
        IErrorHandler errorHandler,
        ILogger<ConnectionStringAspireResourceDiscovery> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _options = options;
        _projector = projector;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
        DiscoverResourcesRequest request,
        CancellationToken cancellationToken)
    {
        DevelopmentEnvironmentGuard.EnsureDevelopment(_hostEnvironment.IsDevelopment(), DevelopmentOnlyMessage);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Value.EnableAspireResourceDiscovery)
        {
            _logger.LogDebug("Aspire resource discovery is disabled. No connection strings will be discovered.");
            return Task.FromResult(new DiscoverResourcesResponse([]));
        }

        try
        {
            var requireLocalConnections = _options.Value.RequireLocalConnections;
            var sections = _configuration.GetSection("ConnectionStrings").GetChildren().ToArray();

            if (sections.Length == 0)
            {
                _logger.LogWarning(
                    "No connection strings found in configuration. Ensure Aspire has injected connection strings " +
                    "via environment variables (e.g. ConnectionStrings__<name>) or appsettings.json.");
            }

            var filteredByLocality = 0;
            var descriptorList = new List<DiscoveredDatabaseResourceDescriptor>(sections.Length);
            foreach (var section in sections)
            {
                var descriptor = CreateDescriptor(section, requireLocalConnections, ref filteredByLocality);
                if (descriptor is not null)
                {
                    descriptorList.Add(descriptor);
                }
            }

            var descriptors = descriptorList.ToArray();

            if (filteredByLocality > 0)
            {
                _logger.LogWarning(
                    "{SkippedCount} connection string(s) were filtered out because RequireLocalConnections=true " +
                    "and the server hostname is not a loopback/local address. In container environments (e.g. Aspire), " +
                    "the database server hostname is the container service name (e.g. 'sampledb'), not 'localhost'. " +
                    "To allow container-hosted databases, set '{ConfigKey}' to false in configuration.",
                    filteredByLocality,
                    $"{DataExplorerOptions.SectionName}:RequireLocalConnections");
            }

            var includeUnavailableResources = request.IncludeUnavailableResources
                ?? _options.Value.IncludeUnavailableResources;

            _logger.LogDebug(
                "Discovered {Count} connection string resource(s). RequireLocalConnections={RequireLocal}.",
                descriptors.Length,
                requireLocalConnections);

            return Task.FromResult(_projector.Project(descriptors, DateTimeOffset.UtcNow, includeUnavailableResources));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw _errorHandler.CreateException(ex, new ErrorContext("discover-resources"));
        }
    }

    private DiscoveredDatabaseResourceDescriptor? CreateDescriptor(
        IConfigurationSection section,
        bool requireLocalConnections,
        ref int filteredByLocality)
    {
        if (string.IsNullOrWhiteSpace(section.Key) || string.IsNullOrWhiteSpace(section.Value))
        {
            return null;
        }

        var key = section.Key.Trim();
        var connectionString = section.Value;

        // When RequireLocalConnections is enabled, refuse to surface any resource whose server
        // is not on the local machine so the tool cannot be pointed at a remote/shared database.
        if (requireLocalConnections && !IsLocalConnection(connectionString))
        {
            filteredByLocality++;
            _logger.LogDebug(
                "Skipping connection string '{Key}': server is not local and RequireLocalConnections=true.",
                key);
            return null;
        }

        var databaseName = TryGetDatabaseName(connectionString) ?? key;
        var providerHint = InferProviderHint(connectionString);

        // Store the resolved connection string directly so the connection provider can use it
        // without a second environment-variable lookup. The env-var name is retained as a
        // fallback for the AppHost-hosted scenario where Aspire injects it as an env var.
        var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["resourceType"] = "ConnectionString",
            ["connectionString"] = connectionString,
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

    private static bool IsLocalConnection(string connectionString)
    {
        string? server = null;
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            if (!TryGetValue(builder, "Server", out server)
                && !TryGetValue(builder, "Data Source", out server)
                && !TryGetValue(builder, "Host", out server))
            {
                // No server key means an in-process/relative source (for example SQLite) — treat as local.
                return true;
            }
        }
        catch (ArgumentException)
        {
            // Unparseable connection strings are treated as non-local so they are not surfaced.
            return false;
        }

        return IsLocalHost(server);
    }

    private static bool IsLocalHost(string? server)
    {
        if (string.IsNullOrWhiteSpace(server))
        {
            return true;
        }

        var host = server.Trim();

        // Bracketed IPv6 literal ("[::1]" or "[::1]:5432"): extract the address inside the brackets.
        if (host.StartsWith('['))
        {
            var closingBracket = host.IndexOf(']');
            if (closingBracket > 0)
            {
                return IsLoopbackOrMachine(host[1..closingBracket]);
            }
        }

        // Strip a leading protocol prefix such as "tcp:" or "np:" (an alphabetic token before ':').
        var protocolSeparator = host.IndexOf(':');
        if (protocolSeparator > 1 && host[..protocolSeparator].All(char.IsLetter))
        {
            host = host[(protocolSeparator + 1)..];
        }

        // Drop a named-instance suffix ("host\\instance").
        var instanceSeparator = host.IndexOf('\\');
        if (instanceSeparator >= 0)
        {
            host = host[..instanceSeparator];
        }

        // Drop a port suffix ("host,1433" for SQL Server or "host:5432" for others).
        var portSeparator = host.IndexOfAny([',', ':']);
        if (portSeparator >= 0)
        {
            host = host[..portSeparator];
        }

        return IsLoopbackOrMachine(host.Trim());
    }

    private static bool IsLoopbackOrMachine(string host)
    {
        if (host.Length == 0
            || host is "."
            || host.StartsWith("(local)", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal))
        {
            return true;
        }

        return host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
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
