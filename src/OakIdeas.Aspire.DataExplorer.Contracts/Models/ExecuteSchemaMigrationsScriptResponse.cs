namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ExecuteSchemaMigrationsScriptResponse(
    string DatabaseName,
    bool Succeeded,
    int BatchCount,
    IReadOnlyList<string> Messages,
    DateTimeOffset ExecutedAt,
    DataExplorerError? Error = null);
