namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Describes why a requested operation was rejected because its governing feature flag is disabled.
/// Consumers surface this through the shared <c>DataExplorerError</c> contract using
/// <c>ErrorCategory.FeatureDisabled</c> rather than raising a dedicated exception type.
/// </summary>
public sealed record FeatureDisabledResult(
    string Key,
    string Message,
    string? ReasonCode = null);

