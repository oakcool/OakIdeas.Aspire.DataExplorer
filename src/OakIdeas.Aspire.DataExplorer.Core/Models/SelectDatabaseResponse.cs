using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record SelectDatabaseResponse(
    bool Succeeded,
    SelectedDatabaseContext? Context,
    string? ErrorMessage,
    DataExplorerError? Error = null);

