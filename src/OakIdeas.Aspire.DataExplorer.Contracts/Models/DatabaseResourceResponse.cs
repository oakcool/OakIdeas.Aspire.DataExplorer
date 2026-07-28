namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DatabaseResourceResponse(
    string Name,
    string Provider,
    string? DisplayName,
    bool IsAvailable);
