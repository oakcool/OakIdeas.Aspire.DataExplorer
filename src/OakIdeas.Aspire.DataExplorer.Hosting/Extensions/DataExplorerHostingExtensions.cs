using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using OakIdeas.Aspire.DataExplorer.Hosting.Services;

namespace OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

public static class DataExplorerHostingExtensions
{
    private const string DevelopmentOnlyMessage = "AddDataExplorer can only be used in Development environments.";

    public static IDistributedApplicationBuilder AddDataExplorer(this IDistributedApplicationBuilder builder)
    {
        DevelopmentEnvironmentGuard.EnsureDevelopment(builder.Environment.IsDevelopment(), DevelopmentOnlyMessage);

        builder.Services.AddOptions<DataExplorerOptions>()
            .Bind(builder.Configuration.GetSection(DataExplorerOptions.SectionName));
        builder.Services.AddSingleton<DiscoveredDatabaseResourceProjector>();
        builder.Services.AddSingleton<IAspireResourceDiscovery, AspireResourceDiscovery>();
        builder.Services.AddSelectedDatabaseService();

        return builder;
    }
}
