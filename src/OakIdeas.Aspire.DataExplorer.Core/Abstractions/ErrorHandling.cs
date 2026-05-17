using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IProviderErrorMapper
{
    DatabaseProviderType ProviderType { get; }

    bool TryMap(Exception exception, ErrorContext context, out DataExplorerError error);
}

public interface IErrorHandler
{
    DataExplorerError CreateError(
        ErrorCategory category,
        string message,
        string? recoverySuggestion,
        ErrorContext context,
        string? diagnosticCode = null);

    DataExplorerError MapException(Exception exception, ErrorContext context);

    DataExplorerOperationException CreateException(Exception exception, ErrorContext context);
}
