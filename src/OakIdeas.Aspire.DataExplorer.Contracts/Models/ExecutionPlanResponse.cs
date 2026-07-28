namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record ExecutionPlanResponse(
    bool IsAvailable,
    string? Provider = null,
    IReadOnlyList<ExecutionPlanNode>? Nodes = null,
    IReadOnlyList<ExecutionPlanEdge>? Edges = null,
    string? RawPlan = null,
    string? Message = null);
