namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record ColumnMetadata(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsIdentity,
    int? MaxLength,
    int? Precision,
    int? Scale);
