namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The result of a correlated span query from the in-memory trace store.
/// </summary>
/// <param name="Spans">The matching correlated spans in descending start-time order.</param>
/// <param name="TotalCount">Total number of matching spans before any <see cref="TraceQueryRequest.MaxSpans"/> cap is applied.</param>
/// <param name="IsTruncated"><see langword="true"/> when more spans matched than were returned.</param>
/// <param name="Error">A sanitized error descriptor when the query itself failed, otherwise <see langword="null"/>.</param>
public sealed record TraceQueryResponse(
    IReadOnlyList<CorrelatedSpan> Spans,
    int TotalCount,
    bool IsTruncated,
    DataExplorerError? Error = null);
