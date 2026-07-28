using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface ISelectedDatabaseService
{
    event EventHandler<SelectedDatabaseContext?>? SelectionChanged;

    Task<SelectDatabaseResponse> SelectDatabaseAsync(
        string resourceId,
        CancellationToken cancellationToken);

    Task<SelectedDatabaseContext?> GetSelectedDatabaseAsync(
        CancellationToken cancellationToken);

    Task ClearSelectionAsync(CancellationToken cancellationToken);

    Task<bool> IsSelectedAsync(CancellationToken cancellationToken);
}
