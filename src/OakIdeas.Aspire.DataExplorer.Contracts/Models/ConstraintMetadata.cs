using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ConstraintMetadata(
    string ConstraintName,
    ConstraintType ConstraintType,
    string TableName,
    string SchemaName,
    string? ColumnName,
    string? Definition,
    bool IsDisabled,
    string ObjectId);
