using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

/// <summary>
/// Extension methods for registering Data Change Timeline services.
/// </summary>
public static class ChangeTimelineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory data change timeline service as a singleton.
    /// Call this method from the application composition root when the
    /// <c>Timeline.DataChangeTimeline</c> feature flag is enabled.
    /// </summary>
    public static IServiceCollection AddChangeTimelineServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IChangeTimelineService, InMemoryChangeTimelineService>();

        return services;
    }
}
