namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The type of data-modifying operation captured in a change timeline event.
/// </summary>
public enum DataChangeOperation
{
    /// <summary>A row was inserted.</summary>
    Insert,

    /// <summary>An existing row was updated.</summary>
    Update,

    /// <summary>A row was deleted.</summary>
    Delete,
}
