namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DataExplorerError(
    ErrorCategory Category,
    string Message,
    string? RecoverySuggestion,
    string Operation,
    string? Target,
    DateTimeOffset Timestamp,
    string? DiagnosticCode = null);

