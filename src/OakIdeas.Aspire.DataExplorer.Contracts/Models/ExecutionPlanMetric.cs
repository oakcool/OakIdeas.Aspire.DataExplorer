namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

/// <summary>A single label/value metric entry in an execution plan operator node.</summary>
public sealed record ExecutionPlanMetric(string Label, string Value);
