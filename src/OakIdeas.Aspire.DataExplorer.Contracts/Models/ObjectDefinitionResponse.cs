using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ObjectDefinitionResponse(
    string? Definition,
    bool IsAvailable,
    string? UnavailableReason = null);

