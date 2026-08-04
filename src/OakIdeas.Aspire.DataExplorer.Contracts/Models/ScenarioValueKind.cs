namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Determines how a column value is produced during scenario execution.
/// </summary>
public enum ScenarioValueKind
{
    /// <summary>A literal value supplied directly in the scenario definition.</summary>
    Fixed = 0,

    /// <summary>A value produced by a named generator at execution time (e.g., <c>guid</c>, <c>utcnow</c>).</summary>
    Generated = 1,

    /// <summary>A value taken from the generated primary key output of a preceding scenario table operation.</summary>
    Reference = 2,
}
