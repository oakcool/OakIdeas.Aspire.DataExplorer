using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Analyses a set of correlated spans and produces advisory diagnostic insights.
/// Insights are heuristic warnings only; they must not be presented as definitive diagnoses.
/// </summary>
public interface ITraceInsightsAnalyzer
{
    /// <summary>
    /// Analyses the provided spans and returns any detected insights.
    /// Returns an empty list when no insights are found.
    /// </summary>
    /// <param name="spans">The spans to analyse. Must not be <see langword="null"/>.</param>
    /// <returns>A read-only list of <see cref="TraceInsight"/> instances, ordered by severity.</returns>
    IReadOnlyList<TraceInsight> Analyze(IReadOnlyList<CorrelatedSpan> spans);
}
