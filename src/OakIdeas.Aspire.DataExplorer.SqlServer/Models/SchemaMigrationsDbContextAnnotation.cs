using Aspire.Hosting.ApplicationModel;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Models;

public sealed record SchemaMigrationsDbContextAnnotation(
    string ProjectPath,
    string DbContextTypeName) : IResourceAnnotation;
