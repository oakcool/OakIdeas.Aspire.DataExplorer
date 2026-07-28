namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

/// <summary>Represents a directed connection from a parent operator to a child input operator in an execution plan.</summary>
public sealed record ExecutionPlanEdge(string ParentId, string ChildId);
