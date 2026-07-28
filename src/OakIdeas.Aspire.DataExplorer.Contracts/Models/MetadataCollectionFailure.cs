using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record MetadataCollectionFailure(
    string Operation,
    string? Target,
    string Message);

