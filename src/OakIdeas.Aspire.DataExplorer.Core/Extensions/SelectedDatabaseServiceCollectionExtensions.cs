using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

public static class SelectedDatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddSelectedDatabaseService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProviderErrorMapper, NullProviderErrorMapper>());
        services.TryAddSingleton<IErrorHandler, ErrorHandler>();
        services.TryAddSingleton<IAspireResourceDiscovery, NullAspireResourceDiscovery>();
        services.AddScoped<ISelectedDatabaseService, SelectedDatabaseService>();
        return services;
    }
}
