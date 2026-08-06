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

    // Default false: Aspire deployments inject connection strings for container-hosted databases
    // (e.g. Server=sampledb,1433) whose hostnames are not localhost. The IsDevelopment() guard
    // is the primary safety net. Set to true to restrict discovery to loopback/machine connections only.
    public bool RequireLocalConnections { get; set; } = false;

    public int DefaultPageSize { get; set; } = 100;

    public int MaxPageSize { get; set; } = 1000;

    public int QueryTimeoutSeconds { get; set; } = 30;

    public int MaxQueryRows { get; set; } = 1000;
}
