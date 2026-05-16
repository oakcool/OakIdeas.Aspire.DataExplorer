using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

namespace OakIdeas.Aspire.DataExplorer.Web.Abstractions;

/// <summary>
/// Provides explorer operations for discovering resources, selecting databases, and retrieving metadata for UI consumption.
/// </summary>
public interface IExplorerService
{
    /// <summary>
    /// Gets all discovered database resources that can be shown in the explorer database picker.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A typed response containing discovered database resources.</returns>
    Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Selects a discovered database resource by resource ID.
    /// </summary>
    /// <param name="resourceId">The resource ID to select.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A typed response that indicates whether selection succeeded and includes validation errors when it fails.</returns>
    Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the currently selected database context for the explorer.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A typed response containing the current selection, if any.</returns>
    Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets cached or freshly aggregated metadata for the currently selected database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A typed response containing metadata and any collection or validation errors.</returns>
    Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes metadata for the currently selected database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A refresh status response for the attempted refresh operation.</returns>
    Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the SQL/object definition for a specific object in the currently selected database.
    /// </summary>
    /// <param name="objectId">The object identifier.</param>
    /// <param name="objectType">The object type.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A typed response containing the definition when available.</returns>
    Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(
        string objectId,
        DatabaseObjectType objectType,
        CancellationToken cancellationToken);
}
