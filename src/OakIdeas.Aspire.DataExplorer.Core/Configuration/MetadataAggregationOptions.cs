namespace OakIdeas.Aspire.DataExplorer.Core.Configuration;

public sealed class MetadataAggregationOptions
{
    public int CacheTtlMinutes { get; set; } = 5;

    public int AggregationTimeoutSeconds { get; set; } = 120;

    public int TransientRetryCount { get; set; } = 2;

    public int RetryDelayMilliseconds { get; set; } = 100;

    public bool EnableBackgroundDefinitionLoading { get; set; } = true;
}
