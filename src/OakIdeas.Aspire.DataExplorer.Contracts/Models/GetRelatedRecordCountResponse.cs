namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response containing the count of related records for a given relationship and key.
/// </summary>
public sealed record GetRelatedRecordCountResponse(
    int Count,
    bool IsTruncated = false);
