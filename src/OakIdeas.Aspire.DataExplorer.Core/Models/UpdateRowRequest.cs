namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record UpdateRowRequest(
    string Schema,
    string Table,
    IReadOnlyDictionary<string, object?> KeyValues,
    IReadOnlyDictionary<string, object?> Values);

