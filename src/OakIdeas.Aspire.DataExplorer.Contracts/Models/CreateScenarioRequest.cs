namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to create a new test data scenario.
/// </summary>
/// <param name="Name">Display name for the new scenario.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Seed">Optional deterministic seed for value generation.</param>
/// <param name="Tables">Ordered table operations to include in the scenario.</param>
public sealed record CreateScenarioRequest(
    string Name,
    string? Description,
    int? Seed,
    IReadOnlyList<ScenarioTableOperation> Tables);
