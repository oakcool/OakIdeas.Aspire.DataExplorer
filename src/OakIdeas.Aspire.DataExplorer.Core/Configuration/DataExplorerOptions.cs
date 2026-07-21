namespace OakIdeas.Aspire.DataExplorer.Core.Configuration;

public sealed class DataExplorerOptions
{
    public const string SectionName = "OakIdeas:Aspire:DataExplorer";

    // Secure by default: writes are the destructive capability, so they are opt-in.
    // When false, queries run inside a rolled-back transaction so no changes persist.
    public bool EnableWriteOperations { get; set; } = false;

    public bool EnableAdHocQueries { get; set; } = true;

    public bool EnableAspireResourceDiscovery { get; set; } = true;

    public bool IncludeUnavailableResources { get; set; } = true;

    public bool RequireLocalConnections { get; set; } = true;

    public int DefaultPageSize { get; set; } = 100;

    public int MaxPageSize { get; set; } = 1000;

    public int QueryTimeoutSeconds { get; set; } = 30;

    public int MaxQueryRows { get; set; } = 1000;
}
