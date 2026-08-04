namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// A versioned, named test data scenario that inserts related records in dependency order.
/// Scenarios are designed to be deterministic, repeatable, and safe for development use only.
/// </summary>
/// <param name="ScenarioId">Unique identifier for this scenario.</param>
/// <param name="Name">Developer-supplied display name for the scenario.</param>
/// <param name="Description">Optional human-readable description of what this scenario creates.</param>
/// <param name="Version">Schema version for forward-compatibility. Currently always 1.</param>
/// <param name="Seed">Optional integer seed for deterministic value generation. When null, values are random.</param>
/// <param name="Tables">Ordered list of table operations to execute. Dependency order is respected.</param>
/// <param name="CreatedAt">UTC timestamp when the scenario was created.</param>
/// <param name="LastModifiedAt">UTC timestamp of the most recent modification, or null if never modified.</param>
/// <param name="LastExecutedAt">UTC timestamp of the most recent successful execution, or null if never run.</param>
public sealed record TestDataScenario(
    string ScenarioId,
    string Name,
    string? Description,
    int Version,
    int? Seed,
    IReadOnlyList<ScenarioTableOperation> Tables,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt,
    DateTimeOffset? LastExecutedAt);
