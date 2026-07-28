using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record QueryExecutionPlanResult(
    bool IsAvailable,
    string? Provider = null,
    IReadOnlyList<ExecutionPlanNode>? Nodes = null,
    IReadOnlyList<ExecutionPlanEdge>? Edges = null,
    string? RawPlan = null,
    string? Message = null);
