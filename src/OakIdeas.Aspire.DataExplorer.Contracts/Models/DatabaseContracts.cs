namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public enum DatabaseProviderType
{
    Unknown = 0,
    SqlServer = 1,
    PostgreSql = 2,
    SQLite = 3,
    MySql = 4,
}

public sealed record ConnectionMetadata(
    IReadOnlyDictionary<string, string?> Properties);

public sealed record DiscoveredDatabaseResource(
    string ResourceId,
    string ResourceName,
    string DatabaseName,
    DatabaseProviderType ProviderType,
    ConnectionMetadata ConnectionMetadata,
    bool IsAvailable,
    DateTimeOffset DiscoveredAt);

public sealed record DiscoverResourcesRequest(
    bool? IncludeUnavailableResources = null);

public sealed record DiscoverResourcesResponse(
    IReadOnlyList<DiscoveredDatabaseResource> Resources);

public sealed record DatabaseResourceResponse(
    string Name,
    string Provider,
    string? DisplayName,
    bool IsAvailable);

public sealed record ColumnMetadataResponse(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsIdentity,
    int? MaxLength,
    int? Precision,
    int? Scale);

public sealed record KeyMetadataResponse(
    string Name,
    string Type,
    IReadOnlyList<string> Columns);

public sealed record TableMetadataResponse(
    string Schema,
    string Name,
    IReadOnlyList<ColumnMetadataResponse> Columns,
    IReadOnlyList<KeyMetadataResponse> Keys);

public sealed record ExecuteQueryRequest(
    string ConnectionName,
    string Sql,
    int MaxRows,
    bool ConfirmDestructiveExecution = false);
