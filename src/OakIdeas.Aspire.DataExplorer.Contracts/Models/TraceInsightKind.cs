namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The kind of diagnostic insight produced by trace analysis.
/// </summary>
public enum TraceInsightKind
{
    /// <summary>
    /// The same SQL statement (after normalization) was executed multiple times
    /// within the analysed span set, indicating a potential caching opportunity.
    /// </summary>
    RepeatedQuery = 1,

    /// <summary>
    /// One or more spans exceeded the slow-call duration threshold.
    /// </summary>
    SlowCall = 2,

    /// <summary>
    /// A pattern consistent with N+1 queries was detected: many short queries to the
    /// same table within a single trace. This is a heuristic warning, not a definitive diagnosis.
    /// </summary>
    LikelyNPlusOne = 3,
}
