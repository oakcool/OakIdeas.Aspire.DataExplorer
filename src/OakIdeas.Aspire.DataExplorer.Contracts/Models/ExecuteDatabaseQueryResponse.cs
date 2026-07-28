namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record ExecuteDatabaseQueryResponse(
    string DatabaseName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int RowCount,
    int? AffectedRowCount,
    TimeSpan Duration,
    bool IsTruncated,
    ExecutionPlanResponse? ExecutionPlan = null,
    DataExplorerError? Error = null);
