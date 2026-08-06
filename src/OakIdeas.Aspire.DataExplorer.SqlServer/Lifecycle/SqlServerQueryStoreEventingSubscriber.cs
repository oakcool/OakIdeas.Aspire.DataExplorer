using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Lifecycle;

internal sealed class SqlServerQueryStoreEventingSubscriber(
    DistributedApplicationModel appModel,
    ILogger<SqlServerQueryStoreEventingSubscriber> logger) : IDistributedApplicationEventingSubscriber
{
    private const int MaxRetryAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing,
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        foreach (var target in SqlServerQueryStoreTargetResolver.GetTargets(appModel))
        {
            eventing.Subscribe<ResourceReadyEvent>(
                target.Database,
                (_, ct) => ConfigureQueryStoreAsync(target, ct));
        }

        return Task.CompletedTask;
    }

    private async Task ConfigureQueryStoreAsync(SqlServerQueryStoreTarget target, CancellationToken cancellationToken)
    {
        var connectionString = await ((IResourceWithConnectionString)target.Database)
            .GetConnectionStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Skipping Query Store enablement for SQL Server database resource {DatabaseResourceName} because no connection string was available.",
                target.Database.Name);
            return;
        }

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = SqlServerQueryStoreCommandFactory.CreateEnableCommand(target.Options);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                logger.LogInformation(
                    "Enabled Query Store for SQL Server database resource {DatabaseResourceName}.",
                    target.Database.Name);

                return;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                if (attempt == MaxRetryAttempts)
                {
                    throw new InvalidOperationException(
                        $"Unable to enable Query Store for SQL Server database resource '{target.Database.Name}' after {MaxRetryAttempts} attempts.",
                        ex);
                }

                logger.LogWarning(
                    ex,
                    "Transient error enabling Query Store for SQL Server database resource {DatabaseResourceName} on attempt {Attempt} of {MaxRetryAttempts}. Retrying.",
                    target.Database.Name,
                    attempt,
                    MaxRetryAttempts);

                await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception exception)
    {
        return exception is TimeoutException
            || exception is SqlException { IsTransient: true };
    }
}
