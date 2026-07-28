namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record DatabaseResource(
    string Name,
    string Provider,
    string ConnectionString,
    bool IsLocal,
    bool IsWritable);
