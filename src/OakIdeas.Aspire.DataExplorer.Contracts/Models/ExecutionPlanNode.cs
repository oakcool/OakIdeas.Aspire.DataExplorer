namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

/// <summary>Represents a single operator node in a query execution plan.</summary>
public sealed record ExecutionPlanNode(
    string Id,
    string PhysicalOp,
    string? LogicalOp,
    string? ObjectName,
    string NodeKind,
    IReadOnlyList<ExecutionPlanMetric> EstimatedMetrics,
    IReadOnlyList<ExecutionPlanMetric> ActualMetrics);
