namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to delete a test data scenario by identifier.
/// </summary>
/// <param name="ScenarioId">The identifier of the scenario to delete.</param>
public sealed record DeleteScenarioRequest(string ScenarioId);
