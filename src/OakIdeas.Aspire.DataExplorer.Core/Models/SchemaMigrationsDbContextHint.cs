namespace OakIdeas.Aspire.DataExplorer.Core.Models;

public sealed record SchemaMigrationsDbContextHint(
    string ProjectPath,
    string DbContextTypeName);
