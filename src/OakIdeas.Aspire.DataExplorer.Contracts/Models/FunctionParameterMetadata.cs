using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record FunctionParameterMetadata(
    string Name,
    string DataType);
