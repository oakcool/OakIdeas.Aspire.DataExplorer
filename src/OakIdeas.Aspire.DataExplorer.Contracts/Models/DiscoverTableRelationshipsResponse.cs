namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response containing all navigable relationships discovered for a table.
/// </summary>
public sealed record DiscoverTableRelationshipsResponse(
    IReadOnlyList<TableRelationship> Relationships);
