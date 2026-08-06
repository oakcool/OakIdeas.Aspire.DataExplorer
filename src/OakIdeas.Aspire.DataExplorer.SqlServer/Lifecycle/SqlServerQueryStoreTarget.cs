using Aspire.Hosting.ApplicationModel;
using OakIdeas.Aspire.DataExplorer.SqlServer.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;

internal sealed record SqlServerQueryStoreTarget(
    SqlServerDatabaseResource Database,
    QueryStoreOptions Options);
