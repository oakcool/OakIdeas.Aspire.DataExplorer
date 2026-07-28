using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IProviderErrorMapper
{
    DatabaseProviderType ProviderType { get; }

    bool TryMap(Exception exception, ErrorContext context, out DataExplorerError error);
}

