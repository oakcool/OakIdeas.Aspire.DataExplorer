namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record SchemaMigrationsOverviewResponse(
    string DatabaseName,
    string? DbContextTypeName,
    IReadOnlyList<SchemaMigrationEntry> Migrations,
    IReadOnlyList<SchemaDriftItem> DriftItems,
    IReadOnlyList<string> Warnings,
    string? ComparisonDatabaseName,
    bool ProjectMetadataAvailable,
    bool CanGenerateScripts,
    bool CanExecuteScripts,
    DataExplorerError? Error = null);
