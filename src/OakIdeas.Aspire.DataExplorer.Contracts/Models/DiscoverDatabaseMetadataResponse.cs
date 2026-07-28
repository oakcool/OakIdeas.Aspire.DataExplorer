using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverDatabaseMetadataResponse(
    DatabaseMetadataRoot Metadata,
    DatabaseMetadata? AggregatedMetadata = null,
    MetadataCollectionStatus CollectionStatus = MetadataCollectionStatus.Success,
    IReadOnlyList<MetadataCollectionFailure>? FailureDetails = null,
    DataExplorerError? Error = null);

