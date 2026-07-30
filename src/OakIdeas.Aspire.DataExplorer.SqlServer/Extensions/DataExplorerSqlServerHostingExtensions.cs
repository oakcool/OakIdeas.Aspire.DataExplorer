using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.SqlServer;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;

public static class DataExplorerSqlServerHostingExtensions
{
    private const string SqlServerProviderEnabledEnvironmentVariable = "OakIdeas__Aspire__DataExplorer__Providers__SqlServer__Enabled";

    public static IResourceBuilder<ExecutableResource> AddSqlServer(
        this IResourceBuilder<ExecutableResource> dataExplorer)
    {
        return dataExplorer.WithEnvironment(SqlServerProviderEnabledEnvironmentVariable, "true");
    }

    public static IResourceBuilder<SqlServerDatabaseResource> WithSchemaMigrationsDbContext(
        this IResourceBuilder<SqlServerDatabaseResource> database,
        string projectPath,
        string dbContextTypeName)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        }

        if (string.IsNullOrWhiteSpace(dbContextTypeName))
        {
            throw new ArgumentException("DbContext type name is required.", nameof(dbContextTypeName));
        }

        return database.WithAnnotation(
            new SchemaMigrationsDbContextHint(
                Path.GetFullPath(projectPath),
                dbContextTypeName.Trim()));
    }
}
