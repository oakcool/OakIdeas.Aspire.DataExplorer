using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverFunctionsResponse(
    IReadOnlyDictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>> FunctionsBySchema);

