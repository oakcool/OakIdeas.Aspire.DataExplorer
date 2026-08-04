namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Lifecycle state of a test data scenario.
/// </summary>
public enum ScenarioState
{
    /// <summary>The scenario has been defined but never executed.</summary>
    Draft = 0,

    /// <summary>The scenario has been executed at least once successfully.</summary>
    Executed = 1,

    /// <summary>The most recent execution attempt failed.</summary>
    Failed = 2,
}
