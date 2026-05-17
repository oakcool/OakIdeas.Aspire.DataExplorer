using Microsoft.Extensions.Logging;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

public sealed class ErrorHandler(
    ILogger<ErrorHandler> logger,
    IEnumerable<IProviderErrorMapper> providerErrorMappers) : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger = logger;
    private readonly IReadOnlyList<IProviderErrorMapper> _providerErrorMappers = providerErrorMappers.ToArray();

    public DataExplorerError CreateError(
        ErrorCategory category,
        string message,
        string? recoverySuggestion,
        ErrorContext context,
        string? diagnosticCode = null)
        => new(
            Category: category,
            Message: message,
            RecoverySuggestion: recoverySuggestion,
            Operation: context.Operation,
            Target: context.Target,
            Timestamp: DateTimeOffset.UtcNow,
            DiagnosticCode: diagnosticCode ?? category.ToString());

    public DataExplorerError MapException(Exception exception, ErrorContext context)
    {
        ArgumentNullException.ThrowIfNull(exception);

        foreach (var mapper in _providerErrorMappers)
        {
            if (mapper.ProviderType == context.ProviderType
                && mapper.TryMap(exception, context, out var mappedError))
            {
                Log(mappedError, exception);
                return mappedError;
            }
        }

        var error = exception switch
        {
            TimeoutException => CreateError(
                ErrorCategory.QueryTimeout,
                "The operation timed out before the database responded.",
                "Retry the operation after the database workload settles.",
                context,
                diagnosticCode: "timeout"),
            UnauthorizedAccessException => CreateError(
                ErrorCategory.PermissionDenied,
                "The current development connection does not have permission to complete this operation.",
                "Use a development account with metadata access or select a different database.",
                context,
                diagnosticCode: "permission-denied"),
            InvalidOperationException ex when ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) => CreateError(
                ErrorCategory.ResourceNotFound,
                "The requested database resource could not be found.",
                "Refresh discovered resources and select the database again.",
                context,
                diagnosticCode: "resource-not-found"),
            InvalidOperationException ex when ex.Message.Contains("connection string", StringComparison.OrdinalIgnoreCase) => CreateError(
                ErrorCategory.ConnectionFailed,
                "A development connection could not be resolved for the selected database.",
                "Verify the Aspire resource is exposing a development connection and try again.",
                context,
                diagnosticCode: "missing-connection"),
            InvalidOperationException ex when ex.Message.Contains("does not support", StringComparison.OrdinalIgnoreCase) => CreateError(
                ErrorCategory.ProviderError,
                "The selected provider does not support this operation.",
                "Select a different object or refresh metadata after updating provider support.",
                context,
                diagnosticCode: "provider-unsupported"),
            _ when context.ProviderType is not DatabaseProviderType.Unknown => CreateError(
                ErrorCategory.ProviderError,
                "The database provider reported an error while completing this operation.",
                "Retry the operation or refresh metadata.",
                context,
                diagnosticCode: "provider-error"),
            _ => CreateError(
                ErrorCategory.UnknownError,
                "The operation could not be completed.",
                "Retry the operation. If the problem continues, refresh the page and try again.",
                context,
                diagnosticCode: "unknown-error"),
        };

        Log(error, exception);
        return error;
    }

    public DataExplorerOperationException CreateException(Exception exception, ErrorContext context)
        => new(MapException(exception, context), exception);

    private void Log(DataExplorerError error, Exception exception)
    {
        _logger.LogError(
            "{Category} error during {Operation} for {Target} at {Timestamp}. DiagnosticCode={DiagnosticCode}. ExceptionType={ExceptionType}",
            error.Category,
            error.Operation,
            error.Target ?? "n/a",
            error.Timestamp,
            error.DiagnosticCode ?? "n/a",
            exception.GetType().FullName);
    }
}
