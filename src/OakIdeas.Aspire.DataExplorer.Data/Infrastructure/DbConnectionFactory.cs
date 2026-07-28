using Microsoft.Data.SqlClient;

namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    public SqlConnection CreateSqlConnection(string connectionString) => new(connectionString);
}
