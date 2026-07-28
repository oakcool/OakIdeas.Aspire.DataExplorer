namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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

