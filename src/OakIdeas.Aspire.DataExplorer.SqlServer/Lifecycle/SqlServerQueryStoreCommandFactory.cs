using OakIdeas.Aspire.DataExplorer.SqlServer.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;

internal static class SqlServerQueryStoreCommandFactory
{
    private const string EnableQueryStoreCommand = """
        ALTER DATABASE CURRENT SET QUERY_STORE = ON;
        """;

    public static string CreateEnableCommand(QueryStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return EnableQueryStoreCommand;
    }
}
