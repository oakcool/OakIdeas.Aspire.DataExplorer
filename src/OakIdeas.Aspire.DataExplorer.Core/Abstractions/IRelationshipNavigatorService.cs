using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Service-level abstraction for the Relationship-Aware Data Navigator.
/// Orchestrates relationship discovery and record navigation for the currently selected database.
/// </summary>
public interface IRelationshipNavigatorService
{
    /// <summary>
    /// Returns all navigable relationships for the specified table in the currently selected database.
    /// Enforces the <c>Navigator.RelationshipAwareNavigator</c> feature flag at the service boundary.
    /// </summary>
    Task<DiscoverTableRelationshipsResponse> DiscoverRelationshipsAsync(
        DiscoverTableRelationshipsRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the count of related records for a specific relationship and key value set.
    /// Used to show count previews before loading the full record set.
    /// Enforces the <c>Navigator.RelationshipAwareNavigator</c> feature flag at the service boundary.
    /// </summary>
    Task<GetRelatedRecordCountResponse> GetRelatedRecordCountAsync(
        GetRelatedRecordCountRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches related records across a specific relationship, with pagination.
    /// Enforces the <c>Navigator.RelationshipAwareNavigator</c> feature flag at the service boundary.
    /// </summary>
    Task<NavigateRelatedRecordsResponse> NavigateRelatedRecordsAsync(
        NavigateRelatedRecordsRequest request,
        CancellationToken cancellationToken);
}
