namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record SchemaMigrationEntry(
    string MigrationId,
    string? ProductVersion,
    SchemaMigrationState State,
    bool KnownToProject,
    bool AppliedToDatabase,
    string? Notes);
