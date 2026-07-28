using Microsoft.Data.SqlClient;

namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure;

public interface IDbConnectionFactory
{
    SqlConnection CreateSqlConnection(string connectionString);
}

