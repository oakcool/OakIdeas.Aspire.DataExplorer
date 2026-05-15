using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class DiscoveredDatabaseResourceProjector
{
    public DiscoverResourcesResponse Project(
        IEnumerable<DiscoveredDatabaseResourceDescriptor> descriptors,
        DateTimeOffset discoveredAt,
        bool includeUnavailableResources)
    {
        var resources = descriptors
            .Select(descriptor => CreateResource(descriptor, discoveredAt))
            .Where(resource => resource is not null)
            .Cast<DiscoveredDatabaseResource>()
            .Where(resource => includeUnavailableResources || resource.IsAvailable)
            .OrderBy(resource => resource.ProviderType)
            .ThenBy(resource => resource.ResourceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(resource => resource.ResourceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DiscoverResourcesResponse(resources);
    }

    private static DiscoveredDatabaseResource? CreateResource(
        DiscoveredDatabaseResourceDescriptor descriptor,
        DateTimeOffset discoveredAt)
    {
        var resourceName = Normalize(descriptor.ResourceName);
        var resourceId = Normalize(descriptor.ResourceId);

        if (resourceName is null && resourceId is null)
        {
            return null;
        }

        resourceName ??= resourceId!;
        resourceId ??= resourceName;

        var databaseName = Normalize(descriptor.DatabaseName) ?? resourceName;

        return new DiscoveredDatabaseResource(
            resourceId,
            resourceName,
            databaseName,
            MapProviderType(descriptor.ProviderHint),
            new ConnectionMetadata(descriptor.ConnectionMetadata ?? new Dictionary<string, string?>()),
            descriptor.IsAvailable,
            discoveredAt);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static DatabaseProviderType MapProviderType(string? providerHint)
    {
        if (string.IsNullOrWhiteSpace(providerHint))
        {
            return DatabaseProviderType.Unknown;
        }

        return providerHint.Trim().ToLowerInvariant() switch
        {
            var value when value.Contains("sqlserver", StringComparison.Ordinal)
                || value.Contains("mssql", StringComparison.Ordinal)
                => DatabaseProviderType.SqlServer,
            var value when value.Contains("postgresql", StringComparison.Ordinal)
                || value.Contains("postgres", StringComparison.Ordinal)
                => DatabaseProviderType.PostgreSql,
            var value when value.Contains("sqlite", StringComparison.Ordinal)
                => DatabaseProviderType.SQLite,
            var value when value.Contains("mysql", StringComparison.Ordinal)
                => DatabaseProviderType.MySql,
            _ => DatabaseProviderType.Unknown,
        };
    }
}
