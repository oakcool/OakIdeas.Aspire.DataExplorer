using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record IndexMetadata(
    string IndexName,
    string TableName,
    string SchemaName,
    bool IsPrimaryKey,
    bool IsUnique,
    bool IsClustered,
    IReadOnlyList<string> Columns,
    IReadOnlyList<string> IncludedColumns,
    string? FilterDefinition,
    string ObjectId);
