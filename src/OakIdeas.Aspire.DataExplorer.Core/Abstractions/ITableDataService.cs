using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface ITableDataService
{
    Task<TablePageResult> GetRowsAsync(
        DatabaseResource resource,
        TableRowsRequest request,
        CancellationToken cancellationToken);

    Task<RowOperationResult> InsertRowAsync(
        DatabaseResource resource,
        InsertRowRequest request,
        CancellationToken cancellationToken);

    Task<RowOperationResult> UpdateRowAsync(
        DatabaseResource resource,
        UpdateRowRequest request,
        CancellationToken cancellationToken);

    Task<RowOperationResult> DeleteRowAsync(
        DatabaseResource resource,
        DeleteRowRequest request,
        CancellationToken cancellationToken);
}

