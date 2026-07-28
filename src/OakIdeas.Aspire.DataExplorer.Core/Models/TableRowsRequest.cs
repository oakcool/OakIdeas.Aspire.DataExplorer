namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record TableRowsRequest(
    string Schema,
    string Table,
    int Page,
    int PageSize);
