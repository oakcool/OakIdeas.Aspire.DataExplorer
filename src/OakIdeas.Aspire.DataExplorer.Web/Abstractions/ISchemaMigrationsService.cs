using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Web.Abstractions;

public interface ISchemaMigrationsService
{
    Task<SchemaMigrationsOverviewResponse> GetOverviewAsync(
        SchemaMigrationsOverviewRequest request,
        CancellationToken cancellationToken);

    Task<GenerateSchemaMigrationsScriptResponse> GenerateScriptAsync(
        GenerateSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken);

    Task<ExecuteSchemaMigrationsScriptResponse> ExecuteScriptAsync(
        ExecuteSchemaMigrationsScriptRequest request,
        CancellationToken cancellationToken);
}
