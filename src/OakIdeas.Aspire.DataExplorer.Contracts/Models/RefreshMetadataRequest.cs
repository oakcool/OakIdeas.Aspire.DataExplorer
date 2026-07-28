namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record RefreshMetadataRequest(
    string ResourceId,
    string DatabaseName);
