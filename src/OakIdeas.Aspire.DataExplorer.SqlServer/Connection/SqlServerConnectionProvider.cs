using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Connection;

public sealed class SqlServerConnectionProvider : ISqlServerConnectionFactory
{
    private const string DevelopmentOnlyMessage =
        "SqlServerConnectionProvider is a development-time-only component and cannot be used outside Development environments.";

    private const string ConnectionStringEnvVarKey = "connectionStringEnvironmentVariable";

    private readonly SqlServerConnectionOptions _options;

    public SqlServerConnectionProvider(
        IHostEnvironment hostEnvironment,
        IOptions<SqlServerConnectionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(options);

        DevelopmentEnvironmentGuard.EnsureDevelopment(hostEnvironment.IsDevelopment(), DevelopmentOnlyMessage);

        _options = options.Value;
    }

    public async Task<SqlConnection> CreateConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));
        }

        var connection = new SqlConnection(connectionString);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds));

        try
        {
            await connection.OpenAsync(cts.Token);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        return connection;
    }

    public async Task<ConnectionValidationResult> ValidateConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ConnectionValidationResult(false, "Connection string is null or empty.");
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ValidationTimeoutSeconds));

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cts.Token);

            return new ConnectionValidationResult(true, null);
        }
        catch (OperationCanceledException)
        {
            return new ConnectionValidationResult(false, "Connection validation timed out.");
        }
        catch (SqlException ex)
        {
            return new ConnectionValidationResult(false, $"SQL Server connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new ConnectionValidationResult(false, $"Connection validation failed: {ex.Message}");
        }
    }

    public async Task<SqlConnection> GetConnectionAsync(
        SelectedDatabaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot create connection: selected database context is invalid. {context.ValidationMessage}");
        }

        var connectionString = ResolveConnectionString(context);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string found for database resource '{context.Resource.ResourceId}'.");
        }

        return await CreateConnectionAsync(connectionString, cancellationToken);
    }

    private static string? ResolveConnectionString(SelectedDatabaseContext context)
    {
        var metadata = context.Resource.ConnectionMetadata.Properties;

        if (metadata.TryGetValue("connectionString", out var direct)
            && !string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (metadata.TryGetValue(ConnectionStringEnvVarKey, out var envVarName)
            && !string.IsNullOrWhiteSpace(envVarName))
        {
            return Environment.GetEnvironmentVariable(envVarName);
        }

        return null;
    }
}
