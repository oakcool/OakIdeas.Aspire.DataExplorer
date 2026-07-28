namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record QueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int RowCount,
    TimeSpan Duration,
    int? AffectedRowCount = null,
    bool IsTruncated = false,
    QueryExecutionPlanResult? ExecutionPlan = null);
