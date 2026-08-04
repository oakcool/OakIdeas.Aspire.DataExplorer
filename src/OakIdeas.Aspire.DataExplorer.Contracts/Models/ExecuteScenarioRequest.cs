namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to execute a test data scenario against the currently selected database.
/// </summary>
/// <param name="ScenarioId">The identifier of the scenario to execute.</param>
/// <param name="SeedOverride">
/// Optional seed override for this execution only. When provided, overrides the scenario-level seed.
/// </param>
public sealed record ExecuteScenarioRequest(
    string ScenarioId,
    int? SeedOverride = null);
