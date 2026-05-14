using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Core.Guards;

namespace OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

public static class DataExplorerHostingExtensions
{
    private const string DevelopmentOnlyMessage = "AddDataExplorer can only be used in Development environments.";

    public static IDistributedApplicationBuilder AddDataExplorer(this IDistributedApplicationBuilder builder)
    {
        DevelopmentEnvironmentGuard.EnsureDevelopment(builder.Environment.IsDevelopment(), DevelopmentOnlyMessage);
        return builder;
    }
}
