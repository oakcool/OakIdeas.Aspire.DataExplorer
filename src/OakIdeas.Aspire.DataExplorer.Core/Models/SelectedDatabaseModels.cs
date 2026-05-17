using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record SelectedDatabaseContext(
    DiscoveredDatabaseResource Resource,
    bool IsValid,
    string? ValidationMessage);

public sealed record SelectDatabaseRequest(
    string ResourceId);

public sealed record SelectDatabaseResponse(
    bool Succeeded,
    SelectedDatabaseContext? Context,
    string? ErrorMessage,
    DataExplorerError? Error = null);
