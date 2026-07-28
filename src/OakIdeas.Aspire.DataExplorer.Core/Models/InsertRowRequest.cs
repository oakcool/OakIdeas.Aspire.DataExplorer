namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record InsertRowRequest(
    string Schema,
    string Table,
    IReadOnlyDictionary<string, object?> Values);

