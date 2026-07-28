namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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

