using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;

public static class DataExplorerSqlServerHostingExtensions
{
    private const string SqlServerProviderEnabledEnvironmentVariable = "OakIdeas__Aspire__DataExplorer__Providers__SqlServer__Enabled";

    public static IResourceBuilder<ExecutableResource> AddSqlServer(
        this IResourceBuilder<ExecutableResource> dataExplorer)
    {
        return dataExplorer.WithEnvironment(SqlServerProviderEnabledEnvironmentVariable, "true");
    }
}
