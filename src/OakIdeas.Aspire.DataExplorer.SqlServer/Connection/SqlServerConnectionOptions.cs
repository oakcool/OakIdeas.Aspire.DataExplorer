namespace OakIdeas.Aspire.DataExplorer.SqlServer.Connection;

public sealed class SqlServerConnectionOptions
{
    public const string SectionName = "OakIdeas:Aspire:DataExplorer:SqlServer";

    public int ConnectionTimeoutSeconds { get; set; } = 30;

    public int ValidationTimeoutSeconds { get; set; } = 10;
}
