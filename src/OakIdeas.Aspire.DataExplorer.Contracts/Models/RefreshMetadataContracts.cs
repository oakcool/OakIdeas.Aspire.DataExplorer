namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum RefreshStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}

public sealed record RefreshMetadataRequest(
    string ResourceId,
    string DatabaseName);

public sealed record RefreshMetadataResponse(
    RefreshStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<string> Errors,
    bool IsPartialSuccess,
    DatabaseMetadataRoot? Metadata,
    DataExplorerError? Error = null);
