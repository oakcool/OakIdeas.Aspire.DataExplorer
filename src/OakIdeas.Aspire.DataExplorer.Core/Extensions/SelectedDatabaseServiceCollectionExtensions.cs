using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Extensions;

public static class SelectedDatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddSelectedDatabaseService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ISelectedDatabaseService, SelectedDatabaseService>();
        return services;
    }
}
