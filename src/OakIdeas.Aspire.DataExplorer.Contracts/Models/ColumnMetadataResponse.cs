namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ColumnMetadataResponse(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsIdentity,
    int? MaxLength,
    int? Precision,
    int? Scale);

