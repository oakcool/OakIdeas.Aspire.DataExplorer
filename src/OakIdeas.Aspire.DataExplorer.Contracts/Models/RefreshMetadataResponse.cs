namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record RefreshMetadataResponse(
    RefreshStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<string> Errors,
    bool IsPartialSuccess,
    DatabaseMetadataRoot? Metadata,
    DataExplorerError? Error = null);

