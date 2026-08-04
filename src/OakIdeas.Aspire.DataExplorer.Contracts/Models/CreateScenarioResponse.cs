namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Response returned after creating a new test data scenario.
/// </summary>
/// <param name="Scenario">The newly created scenario, or <see langword="null"/> on failure.</param>
/// <param name="Success">Whether the scenario was created successfully.</param>
/// <param name="ErrorMessage">Error details when <see cref="Success"/> is <see langword="false"/>.</param>
public sealed record CreateScenarioResponse(
    TestDataScenario? Scenario,
    bool Success,
    string? ErrorMessage = null);
