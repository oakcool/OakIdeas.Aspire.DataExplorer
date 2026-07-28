namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record DeleteRowRequest(
    string Schema,
    string Table,
    IReadOnlyDictionary<string, object?> KeyValues);
