using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record FunctionMetadata(
    string SchemaName,
    string FunctionName,
    FunctionType FunctionType,
    string ObjectId,
    string? ReturnType,
    bool HasDefinitionAvailable,
    DateTimeOffset? CreatedAt,
    IReadOnlyList<FunctionParameterMetadata>? Parameters = null);
