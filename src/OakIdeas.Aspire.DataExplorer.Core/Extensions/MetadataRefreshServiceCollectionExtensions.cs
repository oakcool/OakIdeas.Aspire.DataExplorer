using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

public static class MetadataRefreshServiceCollectionExtensions
{
    public static IServiceCollection AddMetadataRefreshService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMetadataRefreshService, MetadataRefreshService>();
        return services;
    }
}
