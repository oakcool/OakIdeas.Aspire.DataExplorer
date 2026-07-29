using System.Text.RegularExpressions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// Default implementation of <see cref="ITraceInsightsAnalyzer"/>.
/// Detects repeated queries, slow calls, and likely N+1 patterns.
/// All findings are advisory heuristics.
/// </summary>
public sealed partial class TraceInsightsAnalyzer : ITraceInsightsAnalyzer
{
    /// <summary>
    /// Minimum number of identical normalised statements within a span set to flag as repeated.
    /// </summary>
    public const int RepeatedQueryThreshold = 3;

    /// <summary>
    /// Minimum duration in milliseconds to flag a span as a slow call.
    /// </summary>
    public const double SlowCallThresholdMs = 500.0;

    /// <summary>
    /// Minimum number of similar short queries per trace to flag as a likely N+1 pattern.
    /// </summary>
    public const int NPlusOneThreshold = 5;

    /// <summary>
    /// Maximum duration for an individual query to be considered "short" for N+1 detection.
    /// </summary>
    public const double NPlusOneShortQueryMs = 100.0;

    /// <inheritdoc />
    public IReadOnlyList<TraceInsight> Analyze(IReadOnlyList<CorrelatedSpan> spans)
    {
        ArgumentNullException.ThrowIfNull(spans);

        if (spans.Count == 0)
        {
            return [];
        }

        var insights = new List<TraceInsight>();

        DetectRepeatedQueries(spans, insights);
        DetectSlowCalls(spans, insights);
        DetectNPlusOne(spans, insights);

        return insights.AsReadOnly();
    }

    private static void DetectRepeatedQueries(
        IReadOnlyList<CorrelatedSpan> spans,
        List<TraceInsight> insights)
    {
        var groups = spans
            .Where(s => !string.IsNullOrWhiteSpace(s.DbStatement))
            .GroupBy(s => NormalizeStatement(s.DbStatement!), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= RepeatedQueryThreshold)
            .ToArray();

        foreach (var group in groups)
        {
            var count = group.Count();
            var spanIds = group.Select(s => s.SpanId).ToArray();
            insights.Add(new TraceInsight(
                Kind: TraceInsightKind.RepeatedQuery,
                Message: $"The same query was executed {count} times. " +
                         "Consider caching the result or consolidating into a single query.",
                AffectedSpanIds: spanIds));
        }
    }

    private static void DetectSlowCalls(
        IReadOnlyList<CorrelatedSpan> spans,
        List<TraceInsight> insights)
    {
        var slow = spans
            .Where(s => s.Duration.TotalMilliseconds >= SlowCallThresholdMs)
            .ToArray();

        if (slow.Length == 0)
        {
            return;
        }

        var spanIds = slow.Select(s => s.SpanId).ToArray();
        var maxMs = slow.Max(s => s.Duration.TotalMilliseconds);
        insights.Add(new TraceInsight(
            Kind: TraceInsightKind.SlowCall,
            Message: $"{slow.Length} query call(s) exceeded {SlowCallThresholdMs} ms " +
                     $"(slowest: {maxMs:F0} ms). Review query plans or add indexes.",
            AffectedSpanIds: spanIds));
    }

    private static void DetectNPlusOne(
        IReadOnlyList<CorrelatedSpan> spans,
        List<TraceInsight> insights)
    {
        // Group by trace ID then look for many short queries with the same leading statement keyword
        // and target table pattern within that trace.
        var byTrace = spans
            .Where(s => !string.IsNullOrWhiteSpace(s.DbStatement)
                        && s.Duration.TotalMilliseconds < NPlusOneShortQueryMs)
            .GroupBy(s => s.TraceId, StringComparer.OrdinalIgnoreCase);

        foreach (var traceGroup in byTrace)
        {
            // Sub-group by normalised first token + target table to detect N+1 per trace.
            var subGroups = traceGroup
                .GroupBy(s => ExtractStatementSignature(s.DbStatement!), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() >= NPlusOneThreshold)
                .ToArray();

            foreach (var subGroup in subGroups)
            {
                var count = subGroup.Count();
                var spanIds = subGroup.Select(s => s.SpanId).ToArray();
                insights.Add(new TraceInsight(
                    Kind: TraceInsightKind.LikelyNPlusOne,
                    Message: $"Possible N+1 pattern detected: {count} short similar queries " +
                             $"in trace {traceGroup.Key[..Math.Min(8, traceGroup.Key.Length)]}…. " +
                             "This is a heuristic warning. Verify with query plan analysis.",
                    AffectedSpanIds: spanIds));
            }
        }
    }

    /// <summary>
    /// Normalises a SQL statement by masking literals and collapsing whitespace,
    /// producing a canonical form suitable for grouping repeated queries.
    /// </summary>
    internal static string NormalizeStatement(string sql)
    {
        var masked = SqlStatementMasker.Mask(sql);
        return CollapseWhitespace(masked).ToUpperInvariant();
    }

    /// <summary>
    /// Extracts a short signature for N+1 grouping: the first keyword plus any table reference.
    /// </summary>
    private static string ExtractStatementSignature(string sql)
    {
        var trimmed = sql.TrimStart();
        var firstWord = trimmed.Split([' ', '\t', '\r', '\n'], 2, StringSplitOptions.RemoveEmptyEntries)
                               .FirstOrDefault() ?? string.Empty;
        return firstWord.ToUpperInvariant();
    }

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex WhitespacePattern();

    private static string CollapseWhitespace(string value)
        => WhitespacePattern().Replace(value, " ").Trim();
}
