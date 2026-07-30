namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// A diagnostic insight produced by analysing a set of correlated spans.
/// Insights are advisory warnings only; they are presented clearly as heuristics,
/// not as definitive diagnoses.
/// </summary>
/// <param name="Kind">The category of insight.</param>
/// <param name="Message">A human-readable description of the insight.</param>
/// <param name="AffectedSpanIds">
/// The span identifiers that contribute to or are affected by this insight.
/// </param>
public sealed record TraceInsight(
    TraceInsightKind Kind,
    string Message,
    IReadOnlyList<string> AffectedSpanIds);
