using Aspire.Hosting.ApplicationModel;
using OakIdeas.Aspire.DataExplorer.SqlServer.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;

internal static class SqlServerQueryStoreTargetResolver
{
    public static IReadOnlyList<SqlServerQueryStoreTarget> GetTargets(DistributedApplicationModel appModel)
    {
        ArgumentNullException.ThrowIfNull(appModel);

        return appModel.Resources
            .OfType<SqlServerDatabaseResource>()
            .Select(CreateTarget)
            .Where(static target => target is not null)
            .Select(static target => target!)
            .OrderBy(static target => target.Database.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SqlServerQueryStoreTarget? CreateTarget(SqlServerDatabaseResource database)
    {
        var annotation = database.Annotations.OfType<QueryStoreConfigurationAnnotation>().LastOrDefault()
            ?? database.Parent.Annotations.OfType<QueryStoreConfigurationAnnotation>().LastOrDefault();

        return annotation is null
            ? null
            : new SqlServerQueryStoreTarget(database, annotation.Options);
    }
}
