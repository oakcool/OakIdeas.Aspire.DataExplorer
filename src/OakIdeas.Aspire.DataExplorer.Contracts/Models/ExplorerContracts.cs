namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record ExplorerDatabaseSelection(
    string ResourceId,
    string ResourceName,
    string DatabaseName,
    DatabaseProviderType ProviderType,
    bool IsAvailable,
    bool IsValid,
    string? ValidationMessage);

public sealed record GetAvailableDatabasesResponse(
    IReadOnlyList<DiscoveredDatabaseResource> Resources,
    DataExplorerError? Error = null);

public sealed record SelectDatabaseResponse(
    bool Succeeded,
    ExplorerDatabaseSelection? Selection,
    IReadOnlyList<string> ValidationErrors,
    DataExplorerError? Error = null);

public sealed record GetSelectedDatabaseResponse(
    ExplorerDatabaseSelection? Selection);

public sealed record GetDatabaseMetadataResponse(
    DatabaseMetadataRoot? Metadata,
    DatabaseMetadata? AggregatedMetadata,
    MetadataCollectionStatus CollectionStatus,
    IReadOnlyList<MetadataCollectionFailure> FailureDetails,
    IReadOnlyList<string> Errors,
    DataExplorerError? Error = null);

public sealed record GetObjectDefinitionResponse(
    string ObjectId,
    DatabaseObjectType ObjectType,
    string? Definition,
    bool IsAvailable,
    string? UnavailableReason,
    IReadOnlyList<string> Errors,
    DataExplorerError? Error = null);

public sealed record ExecuteDatabaseQueryResponse(
    string DatabaseName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int RowCount,
    int? AffectedRowCount,
    TimeSpan Duration,
    bool IsTruncated,
    DataExplorerError? Error = null);
