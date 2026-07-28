using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record StoredProcedureMetadata(
    string SchemaName,
    string ProcedureName,
    string ObjectId,
    bool HasDefinitionAvailable,
    IReadOnlyList<StoredProcedureParameterMetadata>? Parameters,
    DateTimeOffset? CreatedAt);
