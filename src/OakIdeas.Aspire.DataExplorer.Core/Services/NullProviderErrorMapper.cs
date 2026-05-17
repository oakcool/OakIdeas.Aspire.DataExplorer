using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

internal sealed class NullProviderErrorMapper : IProviderErrorMapper
{
    public DatabaseProviderType ProviderType => DatabaseProviderType.Unknown;

    public bool TryMap(Exception exception, ErrorContext context, out DataExplorerError error)
    {
        error = null!;
        return false;
    }
}
