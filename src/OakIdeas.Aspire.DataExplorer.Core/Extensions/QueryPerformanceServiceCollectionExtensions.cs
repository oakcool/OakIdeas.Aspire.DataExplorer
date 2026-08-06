using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

/// <summary>
/// Extension methods for registering Query Performance Workspace services.
/// </summary>
public static class QueryPerformanceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory (no-op) query performance service as a singleton fallback.
    /// A provider-specific implementation can replace this registration to supply real data.
    /// </summary>
    public static IServiceCollection AddQueryPerformanceServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IQueryPerformanceService, InMemoryQueryPerformanceService>();

        return services;
    }
}
