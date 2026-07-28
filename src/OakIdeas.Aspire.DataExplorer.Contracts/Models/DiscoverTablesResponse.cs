using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DiscoverTablesResponse(
    IReadOnlyList<TableObject> Tables);
