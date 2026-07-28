namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record ExecutionPlanResponse(
    bool IsAvailable,
    string? Provider = null,
    string? MermaidDiagram = null,
    string? RawPlan = null,
    string? Message = null);
