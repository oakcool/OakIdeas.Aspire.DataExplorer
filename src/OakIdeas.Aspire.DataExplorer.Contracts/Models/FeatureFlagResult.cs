namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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

