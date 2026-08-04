using System.Collections.Concurrent;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IScenarioBuilderService"/>.
/// Scenario definitions and execution results are not persisted across restarts.
/// Execution of scenarios simulates the insert pipeline by resolving fixed and generated values
/// and capturing output keys. No actual database writes are performed by this implementation;
/// a provider-backed implementation would delegate to the active database provider.
/// Intended for development-time use only.
/// </summary>
public sealed class InMemoryScenarioBuilderService : IScenarioBuilderService
{
    private readonly Lock _lock = new();
    private readonly LinkedList<TestDataScenario> _scenarios = new();
    private readonly ConcurrentDictionary<string, ExecuteScenarioResponse> _lastResults =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyList<TestDataScenario> Scenarios
    {
        get
        {
            lock (_lock) { return [.. _scenarios]; }
        }
    }

    /// <inheritdoc />
    public TestDataScenario? GetScenario(string scenarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);

        lock (_lock)
        {
            return _scenarios.FirstOrDefault(s =>
                string.Equals(s.ScenarioId, scenarioId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public CreateScenarioResponse CreateScenario(CreateScenarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new CreateScenarioResponse(null, false, "Scenario name must not be empty.");
        }

        var scenarioId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        var scenario = new TestDataScenario(
            ScenarioId: scenarioId,
            Name: request.Name.Trim(),
            Description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Version: 1,
            Seed: request.Seed,
            Tables: request.Tables,
            CreatedAt: now,
            LastModifiedAt: null,
            LastExecutedAt: null);

        lock (_lock)
        {
            _scenarios.AddFirst(scenario);
        }

        return new CreateScenarioResponse(scenario, true);
    }

    /// <inheritdoc />
    public CreateScenarioResponse UpdateScenario(string scenarioId, CreateScenarioRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new CreateScenarioResponse(null, false, "Scenario name must not be empty.");
        }

        lock (_lock)
        {
            var existing = _scenarios.FirstOrDefault(s =>
                string.Equals(s.ScenarioId, scenarioId, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                return new CreateScenarioResponse(null, false, $"Scenario '{scenarioId}' not found.");
            }

            var updated = existing with
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Seed = request.Seed,
                Tables = request.Tables,
                LastModifiedAt = DateTimeOffset.UtcNow,
            };

            ReplaceScenario(existing, updated);

            return new CreateScenarioResponse(updated, true);
        }
    }

    /// <inheritdoc />
    public void DeleteScenario(DeleteScenarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            var node = _scenarios.First;
            while (node is not null)
            {
                if (string.Equals(node.Value.ScenarioId, request.ScenarioId, StringComparison.OrdinalIgnoreCase))
                {
                    _scenarios.Remove(node);
                    break;
                }

                node = node.Next;
            }
        }

        _lastResults.TryRemove(request.ScenarioId, out _);
    }

    /// <inheritdoc />
    public Task<ExecuteScenarioResponse> ExecuteScenarioAsync(
        ExecuteScenarioRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TestDataScenario? scenario;
        lock (_lock)
        {
            scenario = _scenarios.FirstOrDefault(s =>
                string.Equals(s.ScenarioId, request.ScenarioId, StringComparison.OrdinalIgnoreCase));
        }

        if (scenario is null)
        {
            var notFound = new ExecuteScenarioResponse(
                request.ScenarioId,
                Success: false,
                InsertedRows: new Dictionary<string, int>(),
                GeneratedKeys: new Dictionary<string, string?>(),
                ErrorMessage: $"Scenario '{request.ScenarioId}' not found.",
                ExecutedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(notFound);
        }

        var effectiveSeed = request.SeedOverride ?? scenario.Seed;
        var random = effectiveSeed.HasValue ? new Random(effectiveSeed.Value) : Random.Shared;

        var generatedKeys = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var insertedRows = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in scenario.Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableKey = $"{table.SchemaName}.{table.TableName}";

            // Resolve each column's value, capturing any generated output.
            string? capturedKey = null;
            foreach (var col in table.Columns)
            {
                var resolvedValue = ResolveValue(col, generatedKeys, random);

                // Capture the first column marked as the identity output (generated guid/int).
                if (col.ValueKind == ScenarioValueKind.Generated && capturedKey is null)
                {
                    capturedKey = resolvedValue;
                }
            }

            // Record the generated key under the operation's alias if provided.
            if (table.Alias is not null)
            {
                generatedKeys[table.Alias] = capturedKey;
            }

            insertedRows[tableKey] = insertedRows.TryGetValue(tableKey, out var existing) ? existing + 1 : 1;
        }

        var executedAt = DateTimeOffset.UtcNow;

        // Update the last-executed timestamp on the scenario record.
        lock (_lock)
        {
            var current = _scenarios.FirstOrDefault(s =>
                string.Equals(s.ScenarioId, scenario.ScenarioId, StringComparison.OrdinalIgnoreCase));

            if (current is not null)
            {
                ReplaceScenario(current, current with { LastExecutedAt = executedAt });
            }
        }

        var response = new ExecuteScenarioResponse(
            ScenarioId: scenario.ScenarioId,
            Success: true,
            InsertedRows: insertedRows,
            GeneratedKeys: generatedKeys,
            ErrorMessage: null,
            ExecutedAt: executedAt);

        _lastResults[scenario.ScenarioId] = response;

        return Task.FromResult(response);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? ResolveValue(
        ScenarioColumnValue col,
        Dictionary<string, string?> generatedKeys,
        Random random)
    {
        return col.ValueKind switch
        {
            ScenarioValueKind.Fixed => col.FixedValue,

            ScenarioValueKind.Generated => ResolveGenerator(col.GeneratorName, random),

            ScenarioValueKind.Reference =>
                col.ReferenceAlias is not null && generatedKeys.TryGetValue(col.ReferenceAlias, out var refVal)
                    ? refVal
                    : null,

            _ => null,
        };
    }

    private static string? ResolveGenerator(string? generatorName, Random random)
    {
        if (generatorName is null)
        {
            return null;
        }

        var lower = generatorName.Trim().ToLowerInvariant();

        if (lower is "guid" or "newguid" or "uuid")
        {
            return Guid.NewGuid().ToString();
        }

        if (lower is "utcnow" or "now" or "datetime")
        {
            return DateTimeOffset.UtcNow.ToString("O");
        }

        if (lower.StartsWith("randomstring(", StringComparison.Ordinal)
            && lower.EndsWith(')')
            && int.TryParse(lower["randomstring(".Length..^1], out var length)
            && length is > 0 and <= 256)
        {
            const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return string.Create(length, random, static (span, rng) =>
            {
                const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                for (var i = 0; i < span.Length; i++)
                {
                    span[i] = Chars[rng.Next(Chars.Length)];
                }
            });
        }

        if (lower is "randomint" or "int")
        {
            return random.Next(1, int.MaxValue).ToString();
        }

        if (lower is "true" or "false")
        {
            return lower;
        }

        // Unknown generator: return as-is so the caller can see what was specified.
        return generatorName;
    }

    // Must be called while holding _lock.
    private void ReplaceScenario(TestDataScenario original, TestDataScenario replacement)
    {
        var node = _scenarios.First;
        while (node is not null)
        {
            if (ReferenceEquals(node.Value, original))
            {
                node.Value = replacement;
                return;
            }

            node = node.Next;
        }
    }
}
