using Dapper;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;

namespace OakIdeas.Aspire.DataExplorer.Data.Infrastructure;

public sealed class DapperSqlExecutor(IDbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteRowsAsync(
        string connectionString,
        string sql,
        object? parameters,
        DataExplorerOptions options,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = connectionFactory.CreateSqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        CommandDefinition command = new(
            sql,
            parameters,
            commandTimeout: options.QueryTimeoutSeconds,
            cancellationToken: cancellationToken);

        IEnumerable<dynamic> rows = await connection.QueryAsync(command);

        return rows
            .Select(row =>
            {
                IDictionary<string, object?> dictionary = row;
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(dictionary);
            })
            .ToList();
    }
}
