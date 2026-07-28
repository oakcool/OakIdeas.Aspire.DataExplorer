using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.SqlServer.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Circuit-scoped service that snapshots feature flag state for the current Blazor circuit.
/// Evaluations are cached after the first call to <see cref="EnsureLoadedAsync"/> to avoid
/// repeated source calls during a single page render cycle.
/// In-session overrides can be applied via <see cref="SetOverride"/> and reset via <see cref="ResetAllOverrides"/>.
/// </summary>
public sealed class FeatureFlagStateService(
    IFeatureFlagService featureFlagService,
    IFeatureFlagCatalog catalog)
{
    private readonly IFeatureFlagService _featureFlagService = featureFlagService;
    private readonly IFeatureFlagCatalog _catalog = catalog;
    private readonly Dictionary<string, bool> _snapshot = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>
    /// Raised when any flag override changes so UI consumers can re-render.
    /// Subscribers must use <see cref="System.ComponentModel.ISynchronizeInvoke"/> or
    /// <c>InvokeAsync</c> if they need to marshal back to the Blazor render thread.
    /// </summary>
    public event Action? FlagsChanged;

    /// <summary>
    /// Evaluates all registered catalog features and caches the results.
    /// Safe to call multiple times; subsequent calls are no-ops.
    /// </summary>
    public async ValueTask EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
        {
            return;
        }

        foreach (var feature in _catalog.Features)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enabled = await _featureFlagService.IsEnabledAsync(feature, null, cancellationToken).ConfigureAwait(false);
            _snapshot[feature.Key] = enabled;
        }

        _loaded = true;
    }

    /// <summary>
    /// Returns whether the feature is enabled, checking session overrides before the cached snapshot.
    /// Returns <see langword="true"/> (the safe default) when the snapshot has not been loaded yet.
    /// </summary>
    public bool IsEnabled(string featureKey)
    {
        if (_overrides.TryGetValue(featureKey, out var overrideValue))
        {
            return overrideValue;
        }

        return !_snapshot.TryGetValue(featureKey, out var value) || value;
    }

    /// <summary>
    /// Returns the snapshot value (the initial evaluated state before any session override), or
    /// <see langword="null"/> when the snapshot has not been loaded yet.
    /// </summary>
    public bool? GetSnapshotValue(string featureKey)
        => _snapshot.TryGetValue(featureKey, out var value) ? value : null;

    /// <summary>
    /// Returns the current session override for the given feature key, or
    /// <see langword="null"/> when no override has been set (i.e., the flag uses its evaluated default).
    /// </summary>
    public bool? GetOverride(string featureKey)
        => _overrides.TryGetValue(featureKey, out var value) ? value : null;

    /// <summary>
    /// Sets a session override for the given feature key.
    /// Pass <see langword="null"/> to clear the override and revert to the evaluated default.
    /// Raises <see cref="FlagsChanged"/> after applying the change.
    /// </summary>
    public void SetOverride(string featureKey, bool? value)
    {
        if (value is null)
        {
            _overrides.Remove(featureKey);
        }
        else
        {
            _overrides[featureKey] = value.Value;
        }

        FlagsChanged?.Invoke();
    }

    /// <summary>
    /// Clears all session overrides and reverts all flags to their evaluated defaults.
    /// Raises <see cref="FlagsChanged"/> after clearing.
    /// </summary>
    public void ResetAllOverrides()
    {
        _overrides.Clear();
        FlagsChanged?.Invoke();
    }

    /// <summary>Returns <see langword="true"/> when at least one session override is active.</summary>
    public bool HasOverrides => _overrides.Count > 0;

    // ── Explorer ──────────────────────────────────────────────────────────────

    /// <summary>Returns whether the Object Explorer feature is enabled.</summary>
    public bool ObjectExplorerEnabled => IsEnabled(FeatureKeys.ExplorerObjectExplorer);

    /// <summary>Returns whether the Object Details feature is enabled.</summary>
    public bool ObjectDetailsEnabled => IsEnabled(FeatureKeys.ExplorerObjectDetails);

    /// <summary>Returns whether the Views feature is enabled.</summary>
    public bool ViewsEnabled => IsEnabled(FeatureKeys.ExplorerViews);

    /// <summary>Returns whether the Stored Procedures feature is enabled.</summary>
    public bool StoredProceduresEnabled => IsEnabled(FeatureKeys.ExplorerStoredProcedures);

    /// <summary>Returns whether the Functions feature is enabled.</summary>
    public bool FunctionsEnabled => IsEnabled(FeatureKeys.ExplorerFunctions);

    /// <summary>Returns whether the Triggers feature is enabled.</summary>
    public bool TriggersEnabled => IsEnabled(FeatureKeys.ExplorerTriggers);

    /// <summary>Returns whether the Indexes feature is enabled.</summary>
    public bool IndexesEnabled => IsEnabled(FeatureKeys.ExplorerIndexes);

    /// <summary>Returns whether the Constraints feature is enabled.</summary>
    public bool ConstraintsEnabled => IsEnabled(FeatureKeys.ExplorerConstraints);

    /// <summary>Returns whether the Foreign Keys feature is enabled.</summary>
    public bool ForeignKeysEnabled => IsEnabled(FeatureKeys.ExplorerForeignKeys);

    /// <summary>Returns whether the Primary Keys feature is enabled.</summary>
    public bool PrimaryKeysEnabled => IsEnabled(FeatureKeys.ExplorerPrimaryKeys);

    /// <summary>Returns whether the Object Definition feature is enabled.</summary>
    public bool ObjectDefinitionEnabled => IsEnabled(FeatureKeys.ExplorerObjectDefinition);

    /// <summary>Returns whether the Schema and Migrations feature is enabled.</summary>
    public bool SchemaMigrationsEnabled => IsEnabled(FeatureKeys.ExplorerSchemaMigrations);

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>Returns whether the Query Editor feature is enabled.</summary>
    public bool QueryEditorEnabled => IsEnabled(FeatureKeys.QueryEditor);

    /// <summary>Returns whether the Query Auto-Execute feature is enabled.</summary>
    public bool QueryAutoExecuteEnabled => IsEnabled(FeatureKeys.QueryAutoExecute);

    /// <summary>Returns whether the Query Execution Plan feature is enabled.</summary>
    public bool QueryExecutionPlanEnabled => IsEnabled(FeatureKeys.QueryExecutionPlan);

    // ── Diagram ───────────────────────────────────────────────────────────────

    /// <summary>Returns whether the Database Diagram feature is enabled.</summary>
    public bool DatabaseDiagramEnabled => IsEnabled(FeatureKeys.DiagramDatabaseDiagram);

    // ── Data Editing ──────────────────────────────────────────────────────────

    /// <summary>Returns whether the Data Insert feature is enabled.</summary>
    public bool DataInsertEnabled => IsEnabled(FeatureKeys.DataEditingInsert);

    /// <summary>Returns whether the Data Update feature is enabled.</summary>
    public bool DataUpdateEnabled => IsEnabled(FeatureKeys.DataEditingUpdate);

    /// <summary>Returns whether the Data Delete feature is enabled.</summary>
    public bool DataDeleteEnabled => IsEnabled(FeatureKeys.DataEditingDelete);

    // ── SQL Server ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns whether the SQL Server stored procedure capability is enabled.
    /// Returns <see langword="true"/> when the SQL Server provider is not registered (flag not in catalog).
    /// </summary>
    public bool SqlServerStoredProceduresEnabled => IsEnabled(SqlServerFeatureKeys.StoredProcedures);

    /// <summary>
    /// Returns whether the SQL Server function capability is enabled.
    /// Returns <see langword="true"/> when the SQL Server provider is not registered (flag not in catalog).
    /// </summary>
    public bool SqlServerFunctionsEnabled => IsEnabled(SqlServerFeatureKeys.Functions);

    /// <summary>
    /// Returns whether the SQL Server trigger capability is enabled.
    /// Returns <see langword="true"/> when the SQL Server provider is not registered (flag not in catalog).
    /// </summary>
    public bool SqlServerTriggersEnabled => IsEnabled(SqlServerFeatureKeys.Triggers);

    /// <summary>
    /// Returns whether the SQL Server execution plan capability is enabled.
    /// Returns <see langword="true"/> when the SQL Server provider is not registered (flag not in catalog).
    /// </summary>
    public bool SqlServerExecutionPlanEnabled => IsEnabled(SqlServerFeatureKeys.ExecutionPlan);
}
