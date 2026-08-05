namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to create a new named database snapshot (restore point).
/// </summary>
public sealed class CreateSnapshotRequest
{
    /// <summary>The database to snapshot.</summary>
    public required string DatabaseName { get; init; }

    /// <summary>Developer-supplied name for the restore point.</summary>
    public required string Name { get; init; }

    /// <summary>Optional developer notes describing the purpose or context.</summary>
    public string? Notes { get; init; }

    /// <summary>
    /// The provider type associated with the snapshot.
    /// Defaults to <see cref="DatabaseProviderType.Unknown"/> when not specified.
    /// </summary>
    public DatabaseProviderType ProviderType { get; init; } = DatabaseProviderType.Unknown;
}
