namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Lifecycle state for a feature flag. Metadata only in this release; future phases may enforce runtime behavior.
/// </summary>
public enum FeatureLifecycle
{
    Planned = 0,
    Development = 1,
    Preview = 2,
    GenerallyAvailable = 3,
    Deprecated = 4,
    Retired = 5,
}

/// <summary>
/// Category grouping for feature flags, corresponding to application areas.
/// </summary>
public enum FeatureCategory
{
    Unknown = 0,
    Explorer = 1,
    Query = 2,
    Diagram = 3,
    DataEditing = 4,
    Providers = 5,
    Infrastructure = 6,
}

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

/// <summary>
/// Context passed to feature evaluation. Carries environment and resource information without exposing secrets.
/// </summary>
public sealed record FeatureEvaluationContext
{
    /// <summary>Current application environment name, e.g., <c>Development</c>.</summary>
    public string? Environment { get; init; }

    /// <summary>Database resource identifier when evaluation is scoped to a specific resource.</summary>
    public string? ResourceId { get; init; }

    /// <summary>Database provider type when evaluation is scoped to a specific provider.</summary>
    public DatabaseProviderType? ProviderType { get; init; }

    /// <summary>Optional correlation identifier for request-level tracing.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Additional feature-specific attributes. Must not include secrets or credentials.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }

    /// <summary>Returns an empty context with no scoping information.</summary>
    public static FeatureEvaluationContext Empty { get; } = new();
}

/// <summary>
/// Outcome state returned by an individual source provider.
/// </summary>
public enum FeatureFlagSourceOutcome
{
    /// <summary>The source defined the flag and it is enabled.</summary>
    Enabled = 1,

    /// <summary>The source defined the flag and it is disabled.</summary>
    Disabled = 2,

    /// <summary>The source has no definition for this flag. Evaluation continues to the next source.</summary>
    NotDefined = 3,

    /// <summary>The source is temporarily unavailable. Evaluation continues to the next source.</summary>
    SourceUnavailable = 4,

    /// <summary>The source contained an invalid value for this flag. Evaluation continues to the next source.</summary>
    InvalidValue = 5,

    /// <summary>The source encountered an unhandled error. Evaluation continues to the next source.</summary>
    Error = 6,
}

/// <summary>
/// Result returned by a single source provider.
/// </summary>
public sealed record FeatureFlagSourceResult
{
    /// <summary>The source provider that produced this result.</summary>
    public required string SourceName { get; init; }

    /// <summary>Outcome of the source lookup.</summary>
    public required FeatureFlagSourceOutcome Outcome { get; init; }

    /// <summary>The flag value when <see cref="Outcome"/> is <see cref="FeatureFlagSourceOutcome.Enabled"/> or <see cref="FeatureFlagSourceOutcome.Disabled"/>.</summary>
    public bool? Value { get; init; }

    /// <summary>Optional human-readable reason or diagnostic detail.</summary>
    public string? Reason { get; init; }

    public static FeatureFlagSourceResult Enabled(string sourceName) =>
        new() { SourceName = sourceName, Outcome = FeatureFlagSourceOutcome.Enabled, Value = true };

    public static FeatureFlagSourceResult Disabled(string sourceName) =>
        new() { SourceName = sourceName, Outcome = FeatureFlagSourceOutcome.Disabled, Value = false };

    public static FeatureFlagSourceResult NotDefined(string sourceName) =>
        new() { SourceName = sourceName, Outcome = FeatureFlagSourceOutcome.NotDefined };

    public static FeatureFlagSourceResult Unavailable(string sourceName, string? reason = null) =>
        new() { SourceName = sourceName, Outcome = FeatureFlagSourceOutcome.SourceUnavailable, Reason = reason };

    public static FeatureFlagSourceResult Invalid(string sourceName, string? reason = null) =>
        new() { SourceName = sourceName, Outcome = FeatureFlagSourceOutcome.InvalidValue, Reason = reason };

    public static FeatureFlagSourceResult FromError(string sourceName, string? reason = null) =>
        new() { SourceName = sourceName, Outcome = FeatureFlagSourceOutcome.Error, Reason = reason };
}

/// <summary>
/// The result of evaluating a feature flag through the full source pipeline.
/// </summary>
public sealed record FeatureFlagResult
{
    /// <summary>The evaluated feature key.</summary>
    public required string Key { get; init; }

    /// <summary>The effective Boolean value after all sources and catalog default are considered.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>The name of the source that produced the winning value, or <c>CatalogDefault</c> when no source defined the flag.</summary>
    public required string WinningSource { get; init; }

    /// <summary>Whether the catalog default was used because no source defined the flag.</summary>
    public required bool UsedCatalogDefault { get; init; }

    /// <summary>Ordered trace of all sources consulted during evaluation.</summary>
    public required IReadOnlyList<FeatureFlagSourceResult> EvaluationTrace { get; init; }

    /// <summary>Any warnings produced during evaluation, such as invalid source values or source errors.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
