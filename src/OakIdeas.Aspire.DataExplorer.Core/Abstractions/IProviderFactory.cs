using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface IProviderFactory
{
    IMetadataProvider Create(DatabaseProviderType providerType);

    bool TryCreate(DatabaseProviderType providerType, out IMetadataProvider? provider);
}

