namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response containing the related records fetched via a relationship navigation.
/// </summary>
public sealed record NavigateRelatedRecordsResponse(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int TotalCount,
    bool HasMore,
    string GeneratedSql);
