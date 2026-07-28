using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record SelectedDatabaseContext(
    DiscoveredDatabaseResource Resource,
    bool IsValid,
    string? ValidationMessage);

