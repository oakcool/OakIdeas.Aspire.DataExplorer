namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Represents a named restore point captured for a database at a specific point in time.
/// </summary>
public sealed class DatabaseSnapshot
{
    /// <summary>Unique identifier for this snapshot.</summary>
    public required string Id { get; init; }

    /// <summary>Developer-supplied name for the restore point.</summary>
    public required string Name { get; init; }

    /// <summary>Optional developer notes describing the purpose or context of this snapshot.</summary>
    public string? Notes { get; init; }

    /// <summary>The name of the database this snapshot was taken from.</summary>
    public required string DatabaseName { get; init; }

    /// <summary>The provider type that created this snapshot.</summary>
    public required DatabaseProviderType ProviderType { get; init; }

    /// <summary>UTC timestamp when the snapshot was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Estimated storage size of the snapshot in bytes, or <see langword="null"/> when not known.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Current lifecycle state of the snapshot.</summary>
    public SnapshotState State { get; init; } = SnapshotState.Available;

    /// <summary>Error message when <see cref="State"/> is <see cref="SnapshotState.Failed"/>.</summary>
    public string? ErrorMessage { get; init; }
}
