using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record ErrorContext(
    string Operation,
    string? Target = null,
    DatabaseProviderType ProviderType = DatabaseProviderType.Unknown);
