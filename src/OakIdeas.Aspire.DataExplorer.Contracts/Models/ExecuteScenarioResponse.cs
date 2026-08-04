namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The result of executing a test data scenario.
/// </summary>
/// <param name="ScenarioId">The identifier of the executed scenario.</param>
/// <param name="Success">Whether all table operations completed successfully.</param>
/// <param name="InsertedRows">Summary of rows inserted per table, keyed as <c>schema.table</c>.</param>
/// <param name="GeneratedKeys">Generated primary key values captured per table alias.</param>
/// <param name="ErrorMessage">Error details when <see cref="Success"/> is <see langword="false"/>.</param>
/// <param name="ExecutedAt">UTC timestamp when execution completed.</param>
public sealed record ExecuteScenarioResponse(
    string ScenarioId,
    bool Success,
    IReadOnlyDictionary<string, int> InsertedRows,
    IReadOnlyDictionary<string, string?> GeneratedKeys,
    string? ErrorMessage,
    DateTimeOffset ExecutedAt);
