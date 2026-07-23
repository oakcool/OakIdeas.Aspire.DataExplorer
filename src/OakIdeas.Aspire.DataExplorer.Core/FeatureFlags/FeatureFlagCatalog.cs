using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;

/// <summary>
/// In-process feature catalog backed by options registration.
/// Validates key uniqueness and dependency references at startup.
/// </summary>
public sealed class FeatureFlagCatalog : IFeatureFlagCatalog
{
    private readonly IReadOnlyDictionary<string, FeatureFlag> _byKey;

    public FeatureFlagCatalog(IReadOnlyList<FeatureFlag> features)
    {
        ArgumentNullException.ThrowIfNull(features);

        var byKey = new Dictionary<string, FeatureFlag>(StringComparer.OrdinalIgnoreCase);

        foreach (var feature in features)
        {
            ArgumentNullException.ThrowIfNull(feature, nameof(features));
            ArgumentException.ThrowIfNullOrWhiteSpace(feature.Key);

            if (byKey.ContainsKey(feature.Key))
            {
                throw new InvalidOperationException(
                    $"Feature key '{feature.Key}' is registered more than once in the feature catalog.");
            }

            byKey[feature.Key] = feature;
        }

        _byKey = byKey;
        Features = features.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureFlag> Features { get; }

    /// <inheritdoc />
    public FeatureFlag? TryGet(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _byKey.TryGetValue(key, out var feature) ? feature : null;
    }

    /// <inheritdoc />
    public bool TryGet(string key, out FeatureFlag? feature)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _byKey.TryGetValue(key, out feature);
    }

    /// <summary>
    /// Validates that all declared feature dependencies refer to features that are registered in this catalog.
    /// Returns a list of validation errors; an empty list means the catalog is valid.
    /// </summary>
    public IReadOnlyList<string> ValidateDependencies()
    {
        var errors = new List<string>();

        foreach (var feature in Features)
        {
            foreach (var dep in feature.DependsOn)
            {
                if (!_byKey.ContainsKey(dep))
                {
                    errors.Add($"Feature '{feature.Key}' declares dependency '{dep}' which is not registered in the catalog.");
                }
            }
        }

        // Detect dependency cycles using DFS
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var feature in Features)
        {
            DetectCycle(feature.Key, visited, inProgress, errors);
        }

        return errors.AsReadOnly();
    }

    private void DetectCycle(
        string key,
        HashSet<string> visited,
        HashSet<string> inProgress,
        List<string> errors)
    {
        if (visited.Contains(key))
        {
            return;
        }

        if (inProgress.Contains(key))
        {
            errors.Add($"Dependency cycle detected involving feature '{key}'.");
            return;
        }

        if (!_byKey.TryGetValue(key, out var feature))
        {
            return;
        }

        inProgress.Add(key);

        foreach (var dep in feature.DependsOn)
        {
            DetectCycle(dep, visited, inProgress, errors);
        }

        inProgress.Remove(key);
        visited.Add(key);
    }
}
