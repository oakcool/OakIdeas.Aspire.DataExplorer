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
            var requireLocalConnections = _options.Value.RequireLocalConnections;

            var descriptors = _configuration.GetSection("ConnectionStrings")
                .GetChildren()
                .Select(section => CreateDescriptor(section, requireLocalConnections))
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

    private static DiscoveredDatabaseResourceDescriptor? CreateDescriptor(
        IConfigurationSection section,
        bool requireLocalConnections)
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
            return null;
        }

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
