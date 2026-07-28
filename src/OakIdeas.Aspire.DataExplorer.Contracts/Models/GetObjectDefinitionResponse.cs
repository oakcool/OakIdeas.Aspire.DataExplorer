namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record GetObjectDefinitionResponse(
    string ObjectId,
    DatabaseObjectType ObjectType,
    string? Definition,
    bool IsAvailable,
    string? UnavailableReason,
    IReadOnlyList<string> Errors,
    DataExplorerError? Error = null);
