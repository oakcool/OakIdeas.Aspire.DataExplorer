namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record TablePageResult(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int Page,
    int PageSize,
    int Count);

