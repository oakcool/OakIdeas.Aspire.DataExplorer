using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed class DataExplorerOperationException(DataExplorerError error, Exception? innerException = null)
    : Exception(error.Message, innerException)
{
    public DataExplorerError Error { get; } = error;
}

