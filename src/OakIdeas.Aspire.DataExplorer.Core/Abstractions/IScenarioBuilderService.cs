using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Service-level abstraction for the Test Data Scenario Builder.
/// Manages scenario definitions and orchestrates execution against the currently selected database.
/// Implementations must be thread-safe; the service is registered as a singleton.
/// </summary>
public interface IScenarioBuilderService
{
    /// <summary>
    /// Returns all defined scenarios, most recently created first.
    /// </summary>
    IReadOnlyList<TestDataScenario> Scenarios { get; }

    /// <summary>
    /// Returns the scenario with the given identifier, or <see langword="null"/> when not found.
    /// </summary>
    TestDataScenario? GetScenario(string scenarioId);

    /// <summary>
    /// Creates a new scenario from the provided request.
    /// </summary>
    /// <param name="request">The scenario definition. Must not be <see langword="null"/>.</param>
    /// <returns>A response indicating success or failure with the newly created scenario.</returns>
    CreateScenarioResponse CreateScenario(CreateScenarioRequest request);

    /// <summary>
    /// Replaces the definition of an existing scenario.
    /// The <paramref name="scenarioId"/> must match an existing scenario.
    /// </summary>
    /// <param name="scenarioId">The identifier of the scenario to update.</param>
    /// <param name="request">The updated scenario definition. Must not be <see langword="null"/>.</param>
    /// <returns>A response indicating success or failure with the updated scenario.</returns>
    CreateScenarioResponse UpdateScenario(string scenarioId, CreateScenarioRequest request);

    /// <summary>
    /// Removes a scenario by identifier.
    /// If the scenario does not exist, the call is a no-op.
    /// </summary>
    /// <param name="request">The delete request. Must not be <see langword="null"/>.</param>
    void DeleteScenario(DeleteScenarioRequest request);

    /// <summary>
    /// Executes a scenario against the currently selected database.
    /// Inserts are performed in table-operation order. Generated keys are captured and made available
    /// for reference columns in subsequent operations.
    /// </summary>
    /// <param name="request">The execution request. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<ExecuteScenarioResponse> ExecuteScenarioAsync(
        ExecuteScenarioRequest request,
        CancellationToken cancellationToken);
}
