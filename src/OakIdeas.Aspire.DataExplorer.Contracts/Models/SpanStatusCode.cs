namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The status of a correlated OpenTelemetry span.
/// Mirrors the OpenTelemetry span status codes used in instrumentation libraries.
/// </summary>
public enum SpanStatusCode
{
    /// <summary>Status is not set by the instrumentation library.</summary>
    Unset = 0,

    /// <summary>The span completed successfully.</summary>
    Ok = 1,

    /// <summary>The span completed with an error.</summary>
    Error = 2,
}
