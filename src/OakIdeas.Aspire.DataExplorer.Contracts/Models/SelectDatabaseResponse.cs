namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record SelectDatabaseResponse(
    bool Succeeded,
    ExplorerDatabaseSelection? Selection,
    IReadOnlyList<string> ValidationErrors,
    DataExplorerError? Error = null);
