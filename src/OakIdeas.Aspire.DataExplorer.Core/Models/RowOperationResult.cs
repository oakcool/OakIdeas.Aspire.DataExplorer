namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record RowOperationResult(
    bool Succeeded,
    int AffectedRows,
    string? Error = null);

