using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record ProviderRegistration(
    DatabaseProviderType ProviderType,
    Type ProviderImplementationType);
