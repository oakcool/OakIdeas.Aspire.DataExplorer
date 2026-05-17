namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum ErrorCategory
{
    ResourceNotFound = 1,
    ConnectionFailed = 2,
    QueryTimeout = 3,
    PermissionDenied = 4,
    ProviderError = 5,
    UnknownError = 6,
}

public sealed record DataExplorerError(
    ErrorCategory Category,
    string Message,
    string? RecoverySuggestion,
    string Operation,
    string? Target,
    DateTimeOffset Timestamp,
    string? DiagnosticCode = null);
