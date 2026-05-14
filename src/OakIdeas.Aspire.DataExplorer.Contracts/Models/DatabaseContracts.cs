namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

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
    int MaxRows);
