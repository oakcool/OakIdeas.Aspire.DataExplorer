using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Circuit-scoped service that snapshots feature flag state for the current Blazor circuit.
/// Evaluations are cached after the first call to <see cref="EnsureLoadedAsync"/> to avoid
/// repeated source calls during a single page render cycle.
/// </summary>
public sealed class FeatureFlagStateService(IFeatureFlagService featureFlagService)
{
    private readonly IFeatureFlagService _featureFlagService = featureFlagService;
    private readonly Dictionary<string, bool> _snapshot = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>
    /// Evaluates all application features and caches the results.
    /// Safe to call multiple times; subsequent calls are no-ops.
    /// </summary>
    public async ValueTask EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
        {
            return;
        }

        foreach (var feature in ApplicationFeatures.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enabled = await _featureFlagService.IsEnabledAsync(feature, null, cancellationToken).ConfigureAwait(false);
            _snapshot[feature.Key] = enabled;
        }

        _loaded = true;
    }

    /// <summary>
    /// Returns whether the feature is enabled, using the cached snapshot.
    /// Returns <see langword="true"/> (the safe default) when the snapshot has not been loaded yet.
    /// </summary>
    public bool IsEnabled(string featureKey)
        => !_snapshot.TryGetValue(featureKey, out var value) || value;

    /// <summary>Returns whether the Query Editor feature is enabled.</summary>
    public bool QueryEditorEnabled => IsEnabled(FeatureKeys.QueryEditor);

    /// <summary>Returns whether the Query Auto-Execute feature is enabled.</summary>
    public bool QueryAutoExecuteEnabled => IsEnabled(FeatureKeys.QueryAutoExecute);

    /// <summary>Returns whether the Query Execution Plan feature is enabled.</summary>
    public bool QueryExecutionPlanEnabled => IsEnabled(FeatureKeys.QueryExecutionPlan);

    /// <summary>Returns whether the Object Explorer feature is enabled.</summary>
    public bool ObjectExplorerEnabled => IsEnabled(FeatureKeys.ExplorerObjectExplorer);

    /// <summary>Returns whether the Database Diagram feature is enabled.</summary>
    public bool DatabaseDiagramEnabled => IsEnabled(FeatureKeys.DiagramDatabaseDiagram);

    /// <summary>Returns whether the Data Insert feature is enabled.</summary>
    public bool DataInsertEnabled => IsEnabled(FeatureKeys.DataEditingInsert);

    /// <summary>Returns whether the Data Update feature is enabled.</summary>
    public bool DataUpdateEnabled => IsEnabled(FeatureKeys.DataEditingUpdate);

    /// <summary>Returns whether the Data Delete feature is enabled.</summary>
    public bool DataDeleteEnabled => IsEnabled(FeatureKeys.DataEditingDelete);
}
