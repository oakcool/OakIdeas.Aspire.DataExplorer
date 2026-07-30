namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The result of querying the data change event store for a capture session.
/// </summary>
/// <param name="Events">The matching change events, ordered by <see cref="DataChangeEvent.Timestamp"/> descending.</param>
/// <param name="TotalCount">The total number of matching events before any <see cref="DataChangeQueryRequest.MaxEvents"/> cap is applied.</param>
/// <param name="IsTruncated">
/// <see langword="true"/> when <paramref name="Events"/> contains fewer items than <paramref name="TotalCount"/>
/// because the result was capped by <see cref="DataChangeQueryRequest.MaxEvents"/>.
/// </param>
/// <param name="Error">An optional sanitized error message when the query could not be completed. <see langword="null"/> on success.</param>
public sealed record DataChangeQueryResponse(
    IReadOnlyList<DataChangeEvent> Events,
    int TotalCount,
    bool IsTruncated,
    string? Error = null);
