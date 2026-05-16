using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

public static class MetadataRefreshServiceCollectionExtensions
{
    public static IServiceCollection AddMetadataRefreshService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<MetadataAggregationOptions>();
        services.TryAddSingleton<IMetadataCache, InMemoryMetadataCache>();
        services.TryAddSingleton<IMetadataAggregationService, MetadataAggregationService>();
        services.TryAddSingleton<IMetadataRefreshService, MetadataRefreshService>();
        return services;
    }
}
