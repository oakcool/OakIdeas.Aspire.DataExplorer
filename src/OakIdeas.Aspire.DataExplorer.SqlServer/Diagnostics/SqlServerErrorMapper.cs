using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Diagnostics;

public sealed class SqlServerErrorMapper : IProviderErrorMapper
{
    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    public bool TryMap(Exception exception, ErrorContext context, out DataExplorerError error)
    {
        if (exception is not SqlException sqlException)
        {
            error = null!;
            return false;
        }

        error = sqlException.Number switch
        {
            -2 => CreateError(
                ErrorCategory.QueryTimeout,
                "The database did not respond before the operation timed out.",
                "Retry the operation after the database workload settles.",
                context,
                "sql-timeout"),
            53 or 4060 => CreateError(
                ErrorCategory.ConnectionFailed,
                "The selected database is currently unavailable.",
                "Confirm the database is running and reachable, then refresh and try again.",
                context,
                "sql-unavailable"),
            18456 => CreateError(
                ErrorCategory.ConnectionFailed,
                "The development database connection was rejected.",
                "Verify the development connection configuration exposed by Aspire and try again.",
                context,
                "sql-login-failed"),
            229 or 230 or 297 or 300 or 916 => CreateError(
                ErrorCategory.PermissionDenied,
                "The development connection does not have permission to read the requested metadata.",
                "Use a development account with metadata access or select a different database.",
                context,
                "sql-permission-denied"),
            207 or 208 => CreateError(
                ErrorCategory.ResourceNotFound,
                "A database object referenced in the query or metadata discovery could not be found. This may indicate missing permissions or the object was recently deleted.",
                "Verify the database object exists and the development connection has permission to access it. Refresh metadata and try again.",
                context,
                $"sql-{sqlException.Number}"),
            _ => CreateError(
                ErrorCategory.ProviderError,
                "SQL Server reported an error while completing this operation.",
                "Retry the operation or refresh metadata.",
                context,
                $"sql-{sqlException.Number}"),
        };

        return true;
    }

    private static DataExplorerError CreateError(
        ErrorCategory category,
        string message,
        string? recoverySuggestion,
        ErrorContext context,
        string diagnosticCode)
        => new(
            Category: category,
            Message: message,
            RecoverySuggestion: recoverySuggestion,
            Operation: context.Operation,
            Target: context.Target,
            Timestamp: DateTimeOffset.UtcNow,
            DiagnosticCode: diagnosticCode);
}
