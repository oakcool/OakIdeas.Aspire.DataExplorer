using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Connection;

public interface ISqlServerConnectionFactory
{
    Task<SqlConnection> CreateConnectionAsync(string connectionString, CancellationToken cancellationToken);

    Task<ConnectionValidationResult> ValidateConnectionAsync(string connectionString, CancellationToken cancellationToken);

    Task<SqlConnection> GetConnectionAsync(SelectedDatabaseContext context, CancellationToken cancellationToken);
}
