using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;
using OakIdeas.Aspire.DataExplorer.SqlServer.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;

public static class DataExplorerSqlServerHostingExtensions
{
    private const string SqlServerProviderEnabledEnvironmentVariable = "OakIdeas__Aspire__DataExplorer__Providers__SqlServer__Enabled";

    public static IResourceBuilder<ExecutableResource> AddSqlServer(
        this IResourceBuilder<ExecutableResource> dataExplorer)
    {
        return dataExplorer.WithEnvironment(SqlServerProviderEnabledEnvironmentVariable, "true");
    }

    /// <summary>
    /// Enables SQL Server Query Store for all databases added to the SQL Server resource.
    /// </summary>
    /// <param name="sqlServer">The SQL Server resource builder.</param>
    /// <param name="options">
    /// Optional Query Store configuration metadata. The current implementation enables Query Store
    /// using SQL Server defaults and reserves this parameter for future strongly typed options.
    /// </param>
    /// <returns>The SQL Server resource builder for fluent chaining.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithQueryStore(
        this IResourceBuilder<SqlServerServerResource> sqlServer,
        QueryStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sqlServer);

        RegisterQueryStoreLifecycleHook(sqlServer.ApplicationBuilder);

        return sqlServer.WithAnnotation(
            new QueryStoreConfigurationAnnotation(options ?? new QueryStoreOptions()),
            ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Enables SQL Server Query Store for the specific database resource.
    /// </summary>
    /// <param name="database">The SQL Server database resource builder.</param>
    /// <param name="options">
    /// Optional Query Store configuration metadata. The current implementation enables Query Store
    /// using SQL Server defaults and reserves this parameter for future strongly typed options.
    /// </param>
    /// <returns>The database resource builder for fluent chaining.</returns>
    public static IResourceBuilder<SqlServerDatabaseResource> WithQueryStore(
        this IResourceBuilder<SqlServerDatabaseResource> database,
        QueryStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        RegisterQueryStoreLifecycleHook(database.ApplicationBuilder);

        return database.WithAnnotation(
            new QueryStoreConfigurationAnnotation(options ?? new QueryStoreOptions()),
            ResourceAnnotationMutationBehavior.Replace);
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
            new SchemaMigrationsDbContextAnnotation(
                Path.GetFullPath(projectPath),
                dbContextTypeName.Trim()));
    }

    private static void RegisterQueryStoreLifecycleHook(IDistributedApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.TryAddEventingSubscriber<SqlServerQueryStoreEventingSubscriber>();
    }
}
