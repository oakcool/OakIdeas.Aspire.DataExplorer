using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class SchemaMigrationsPageTests : BunitContext
{
    [Fact]
    public void DisabledFlag_ShowsUnavailableMessage()
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(false));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<IExplorerService>(new FakeExplorerService());
        Services.AddSingleton<ISchemaMigrationsService>(new FakeSchemaMigrationsService());

        var component = Render<SchemaMigrationsPage>();

        component.Markup.Should().Contain("Schema and Migrations Unavailable");
        component.Markup.Should().NotContain("dotnet ef migrations list");
    }

    [Fact]
    public void EnabledFlag_ShowsCommandTemplatesAndSelectedDatabase()
    {
        Services.AddSingleton<IFeatureFlagService>(new FixedFeatureFlagService(true));
        Services.AddSingleton<IFeatureFlagCatalog>(new FeatureFlagCatalog(ApplicationFeatures.All.ToList()));
        Services.AddScoped<FeatureFlagStateService>();
        Services.AddSingleton<IExplorerService>(new FakeExplorerService());
        Services.AddSingleton<ISchemaMigrationsService>(new FakeSchemaMigrationsService());

        var component = Render<SchemaMigrationsPage>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("Schema and Migrations");
            component.Markup.Should().Contain("applicationdb");
            component.Markup.Should().Contain("Pending script");
            component.Markup.Should().Contain("Schema Drift");
            component.Markup.Should().Contain("20240101000000_InitialCreate");
        });
    }

    private sealed class FixedFeatureFlagService(bool enabled) : IFeatureFlagService
    {
        public ValueTask<FeatureFlagResult> EvaluateAsync(
            FeatureFlag feature,
            FeatureEvaluationContext context,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagResult
            {
                Key = feature.Key,
                IsEnabled = string.Equals(feature.Key, FeatureKeys.ExplorerSchemaMigrations, StringComparison.Ordinal)
                    ? enabled
                    : true,
                WinningSource = "Test",
                UsedCatalogDefault = false,
                EvaluationTrace = [],
            });

        public ValueTask<bool> IsEnabledAsync(
            FeatureFlag feature,
            FeatureEvaluationContext? context = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                string.Equals(feature.Key, FeatureKeys.ExplorerSchemaMigrations, StringComparison.Ordinal)
                    ? enabled
                    : true);
    }

    private sealed class FakeExplorerService : IExplorerService
    {
        public Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetAvailableDatabasesResponse([
                new DiscoveredDatabaseResource(
                    "sql-main",
                    "sql-main",
                    "applicationdb",
                    DatabaseProviderType.SqlServer,
                    new ConnectionMetadata(new Dictionary<string, string?>()),
                    true,
                    DateTimeOffset.UtcNow)
            ]));

        public Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
            => Task.FromResult(new SelectDatabaseResponse(
                Succeeded: false,
                Selection: null,
                ValidationErrors: ["Not implemented for this test."]));

        public Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetSelectedDatabaseResponse(new ExplorerDatabaseSelection(
                ResourceId: "sql-main",
                ResourceName: "sql-main",
                DatabaseName: "applicationdb",
                ProviderType: DatabaseProviderType.SqlServer,
                IsAvailable: true,
                IsValid: true,
                ValidationMessage: null)));

        public Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new GetDatabaseMetadataResponse(Metadata: null, AggregatedMetadata: null, CollectionStatus: MetadataCollectionStatus.Success, FailureDetails: [], Errors: []));

        public Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken)
            => Task.FromResult(new RefreshMetadataResponse(RefreshStatus.Completed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], false, null));

        public Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(
            string objectId,
            DatabaseObjectType objectType,
            CancellationToken cancellationToken)
            => Task.FromResult(new GetObjectDefinitionResponse(objectId, objectType, null, false, null, []));

        public Task<GetDatabaseMetadataResponse> GetDiagramDataAsync(CancellationToken cancellationToken)
            => GetDatabaseMetadataAsync(cancellationToken);

        public Task<ExecuteDatabaseQueryResponse> ExecuteQueryAsync(string sql, bool includeExecutionPlan, bool readOnly, CancellationToken cancellationToken)
            => Task.FromResult(new ExecuteDatabaseQueryResponse("applicationdb", [], [], 0, null, TimeSpan.Zero, false));
    }

    private sealed class FakeSchemaMigrationsService : ISchemaMigrationsService
    {
        public Task<SchemaMigrationsOverviewResponse> GetOverviewAsync(SchemaMigrationsOverviewRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new SchemaMigrationsOverviewResponse(
                "applicationdb",
                "OakIdeas.Aspire.DataExplorer.Sample.Api.Data.SampleDbContext",
                [
                    new SchemaMigrationEntry(
                        "20240101000000_InitialCreate",
                        "10.0.10",
                        SchemaMigrationState.Applied,
                        true,
                        true,
                        null)
                ],
                [
                    new SchemaDriftItem(
                        SchemaDriftSource.LiveVsModel,
                        SchemaDriftSeverity.Additive,
                        "Column",
                        "dbo.TodoItems.UpdatedAt",
                        "Expected column is missing from the live database.")
                ],
                [],
                null,
                true,
                true,
                true));

        public Task<GenerateSchemaMigrationsScriptResponse> GenerateScriptAsync(GenerateSchemaMigrationsScriptRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new GenerateSchemaMigrationsScriptResponse(
                "applicationdb",
                "SELECT 1;",
                request.Kind,
                request.Kind == SchemaScriptKind.Idempotent,
                []));

        public Task<ExecuteSchemaMigrationsScriptResponse> ExecuteScriptAsync(ExecuteSchemaMigrationsScriptRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ExecuteSchemaMigrationsScriptResponse(
                "applicationdb",
                true,
                1,
                ["Executed 1 schema batch(es)."],
                DateTimeOffset.UtcNow));
    }
}
