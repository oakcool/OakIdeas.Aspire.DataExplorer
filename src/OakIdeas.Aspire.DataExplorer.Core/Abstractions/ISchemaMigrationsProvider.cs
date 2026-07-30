using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

public interface ISchemaMigrationsProvider
{
    DatabaseProviderType ProviderType { get; }

    Task<SchemaMigrationsOverviewResponse> GetOverviewAsync(
        DatabaseResource resource,
        ConnectionMetadata connectionMetadata,
        DatabaseMetadata liveMetadata,
        DatabaseMetadata? comparisonMetadata,
        string? comparisonDatabaseName,
        CancellationToken cancellationToken);

    Task<GenerateSchemaMigrationsScriptResponse> GenerateScriptAsync(
        DatabaseResource resource,
        ConnectionMetadata connectionMetadata,
        GenerateSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken);

    Task<ExecuteSchemaMigrationsScriptResponse> ExecuteScriptAsync(
        DatabaseResource resource,
        ExecuteSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken);
}
