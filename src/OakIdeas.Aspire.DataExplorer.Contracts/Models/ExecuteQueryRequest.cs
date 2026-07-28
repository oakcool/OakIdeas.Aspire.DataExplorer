namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ExecuteQueryRequest(
    string ConnectionName,
    string Sql,
    int MaxRows,
    int? TimeoutSeconds = null,
    bool IncludeExecutionPlan = false,
    bool ReadOnly = false);

