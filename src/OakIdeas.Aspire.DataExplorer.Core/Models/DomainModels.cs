namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record DatabaseResource(
    string Name,
    string Provider,
    string ConnectionString,
    bool IsLocal,
    bool IsWritable);

public sealed record SchemaMetadata(
    string Name,
    IReadOnlyList<TableMetadata> Tables,
    IReadOnlyList<ViewMetadata> Views);

public sealed record ViewMetadata(
    string Schema,
    string Name);

public sealed record TableMetadata(
    string Schema,
    string Name,
    IReadOnlyList<ColumnMetadata> Columns,
    IReadOnlyList<KeyMetadata> Keys,
    IReadOnlyList<RelationshipMetadata> Relationships);

public sealed record ColumnMetadata(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsIdentity,
    int? MaxLength,
    int? Precision,
    int? Scale);

public sealed record KeyMetadata(
    string Name,
    string Type,
    IReadOnlyList<string> Columns);

public sealed record RelationshipMetadata(
    string Name,
    string FromSchema,
    string FromTable,
    string ToSchema,
    string ToTable,
    IReadOnlyList<string> FromColumns,
    IReadOnlyList<string> ToColumns);

public sealed record QueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int RowCount,
    TimeSpan Duration,
    int? AffectedRowCount = null,
    bool IsTruncated = false,
    QueryExecutionPlanResult? ExecutionPlan = null);

public sealed record QueryExecutionPlanResult(
    bool IsAvailable,
    string? Provider = null,
    string? MermaidDiagram = null,
    string? RawPlan = null,
    string? Message = null);

public sealed record TableRowsRequest(
    string Schema,
    string Table,
    int Page,
    int PageSize);

public sealed record TablePageResult(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int Page,
    int PageSize,
    int Count);

public sealed record InsertRowRequest(
    string Schema,
    string Table,
    IReadOnlyDictionary<string, object?> Values);

public sealed record UpdateRowRequest(
    string Schema,
    string Table,
    IReadOnlyDictionary<string, object?> KeyValues,
    IReadOnlyDictionary<string, object?> Values);

public sealed record DeleteRowRequest(
    string Schema,
    string Table,
    IReadOnlyDictionary<string, object?> KeyValues);

public sealed record RowOperationResult(
    bool Succeeded,
    int AffectedRows,
    string? Error = null);
