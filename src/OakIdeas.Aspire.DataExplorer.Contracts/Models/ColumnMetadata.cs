using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ColumnMetadata(
    string Name,
    int Ordinal,
    string DataType,
    int? MaxLength,
    int? Precision,
    int? Scale,
    bool IsNullable,
    bool IsIdentity,
    bool IsComputed,
    string? DefaultValue,
    string? Description,
    IReadOnlyDictionary<string, object?> ProviderMetadata);
