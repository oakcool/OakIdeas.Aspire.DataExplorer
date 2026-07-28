namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Defines a feature and its default behavior. Instances are registered in the feature catalog at startup.
/// </summary>
public sealed record FeatureFlag
{
    /// <summary>Stable string key in the form <c>Area.Capability</c>, e.g., <c>Explorer.ObjectExplorer</c>.</summary>
    public required string Key { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Short description of what this feature does.</summary>
    public required string Description { get; init; }

    /// <summary>Application area that owns this feature.</summary>
    public required FeatureCategory Category { get; init; }

    /// <summary>Default value when no source defines the flag.</summary>
    public required bool DefaultEnabled { get; init; }

    /// <summary>Lifecycle state. Informational only in this release.</summary>
    public FeatureLifecycle Lifecycle { get; init; } = FeatureLifecycle.GenerallyAvailable;

    /// <summary>Optional keys of features that must be enabled for this feature to function.</summary>
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    /// <summary>Owner or owning area identifier. Used for governance and cleanup.</summary>
    public string? Owner { get; init; }
}
