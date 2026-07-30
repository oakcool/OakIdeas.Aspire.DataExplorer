using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Provider-specific contract for discovering and navigating table relationships.
/// Implementations are registered per database provider (e.g., SQL Server) and
/// resolve actual foreign key metadata into navigable <see cref="TableRelationship"/> instances.
/// </summary>
public interface IRelationshipNavigationProvider
{
    /// <summary>The database provider type this implementation handles.</summary>
    DatabaseProviderType ProviderType { get; }

    /// <summary>
    /// Discovers all navigable relationships (parent, child, and many-to-many) for the specified table.
    /// </summary>
    Task<DiscoverTableRelationshipsResponse> DiscoverTableRelationshipsAsync(
        DatabaseResource resource,
        DiscoverTableRelationshipsRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the count of related records for a specific relationship and key value set.
    /// Used to show count previews before loading the full record set.
    /// </summary>
    Task<GetRelatedRecordCountResponse> GetRelatedRecordCountAsync(
        DatabaseResource resource,
        GetRelatedRecordCountRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches the related records for a specific relationship and key value set, with pagination.
    /// </summary>
    Task<NavigateRelatedRecordsResponse> NavigateRelatedRecordsAsync(
        DatabaseResource resource,
        NavigateRelatedRecordsRequest request,
        CancellationToken cancellationToken);
}
