using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverStoredProceduresResponse(
    IReadOnlyDictionary<string, IReadOnlyList<StoredProcedureMetadata>> ProceduresBySchema);
