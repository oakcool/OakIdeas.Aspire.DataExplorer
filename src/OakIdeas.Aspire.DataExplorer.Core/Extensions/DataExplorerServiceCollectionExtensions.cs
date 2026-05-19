using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

public static class DataExplorerServiceCollectionExtensions
{
    public static IServiceCollection AddAspireResourceDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DataExplorerOptions>()
            .Bind(configuration.GetSection(DataExplorerOptions.SectionName));
        services.AddSingleton(configuration);
        services.AddSingleton<DiscoveredDatabaseResourceProjector>();
        services.AddSingleton<IAspireResourceDiscovery, ConnectionStringAspireResourceDiscovery>();

        return services;
    }
}