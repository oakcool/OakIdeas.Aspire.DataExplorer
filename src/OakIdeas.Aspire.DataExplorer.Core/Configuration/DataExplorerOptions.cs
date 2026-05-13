namespace OakIdeas.Aspire.DataExplorer.Core.Configuration;

public sealed class DataExplorerOptions
{
    public bool EnableWriteOperations { get; set; } = true;

    public bool EnableAdHocQueries { get; set; } = true;

    public bool RequireLocalConnections { get; set; } = true;

    public int DefaultPageSize { get; set; } = 100;

    public int MaxPageSize { get; set; } = 1000;

    public int QueryTimeoutSeconds { get; set; } = 30;

    public int MaxQueryRows { get; set; } = 1000;
}
