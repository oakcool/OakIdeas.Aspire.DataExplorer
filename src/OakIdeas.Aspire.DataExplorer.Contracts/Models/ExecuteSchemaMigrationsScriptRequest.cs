namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ExecuteSchemaMigrationsScriptRequest(
    string Script,
    string ConfirmationText);
