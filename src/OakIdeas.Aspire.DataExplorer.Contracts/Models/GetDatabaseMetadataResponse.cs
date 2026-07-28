namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record GetDatabaseMetadataResponse(
    DatabaseMetadataRoot? Metadata,
    DatabaseMetadata? AggregatedMetadata,
    MetadataCollectionStatus CollectionStatus,
    IReadOnlyList<MetadataCollectionFailure> FailureDetails,
    IReadOnlyList<string> Errors,
    DataExplorerError? Error = null);
