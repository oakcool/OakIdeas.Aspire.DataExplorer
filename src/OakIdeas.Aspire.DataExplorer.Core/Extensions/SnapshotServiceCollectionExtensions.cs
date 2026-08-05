using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

/// <summary>
/// Extension methods for registering Database Snapshots services.
/// </summary>
public static class SnapshotServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory snapshot service as a singleton.
    /// Call this method from the application composition root to enable the
    /// <c>Snapshots.DatabaseSnapshots</c> feature.
    /// </summary>
    public static IServiceCollection AddSnapshotServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISnapshotService, InMemorySnapshotService>();

        return services;
    }
}
