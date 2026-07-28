namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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

