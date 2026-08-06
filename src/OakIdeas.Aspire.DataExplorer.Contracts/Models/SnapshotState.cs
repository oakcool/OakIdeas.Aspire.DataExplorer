namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Represents the lifecycle state of a database snapshot (restore point).
/// </summary>
public enum SnapshotState
{
    /// <summary>The snapshot is available for comparison or restore.</summary>
    Available = 0,

    /// <summary>The snapshot is being created.</summary>
    Creating = 1,

    /// <summary>A restore from this snapshot is in progress.</summary>
    Restoring = 2,

    /// <summary>The snapshot creation or restore failed.</summary>
    Failed = 3,
}
