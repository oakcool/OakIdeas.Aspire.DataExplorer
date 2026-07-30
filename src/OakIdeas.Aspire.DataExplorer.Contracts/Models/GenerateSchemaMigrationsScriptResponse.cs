namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record GenerateSchemaMigrationsScriptResponse(
    string DatabaseName,
    string Script,
    SchemaScriptKind Kind,
    bool IsIdempotent,
    IReadOnlyList<string> Warnings,
    DataExplorerError? Error = null);
