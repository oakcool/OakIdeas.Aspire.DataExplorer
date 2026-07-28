namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record QueryExecutionPlanResult(
    bool IsAvailable,
    string? Provider = null,
    string? MermaidDiagram = null,
    string? RawPlan = null,
    string? Message = null);
