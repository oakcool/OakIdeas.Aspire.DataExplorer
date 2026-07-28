namespace OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;

public sealed record ExplorerDatabaseSelection(
    string ResourceId,
    string ResourceName,
    string DatabaseName,
    DatabaseProviderType ProviderType,
    bool IsAvailable,
    bool IsValid,
    string? ValidationMessage);
