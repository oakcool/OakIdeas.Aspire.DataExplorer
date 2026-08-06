using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using OakIdeas.Aspire.DataExplorer.SqlServer.Diagnostics;
using OakIdeas.Aspire.DataExplorer.SqlServer.FeatureFlags;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components;
using OakIdeas.Aspire.DataExplorer.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var sqlServerProviderEnabled = builder.Configuration.GetValue<bool>("OakIdeas:Aspire:DataExplorer:Providers:SqlServer:Enabled");

DevelopmentEnvironmentGuard.EnsureDevelopment(
    builder.Environment.IsDevelopment(),
    "OakIdeas.Aspire.DataExplorer is a development-time-only tool and cannot run outside Development.");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

if (sqlServerProviderEnabled)
{
    builder.Services.AddSingleton<SqlServerDatabaseProvider>();
    builder.Services.AddSingleton<IProviderErrorMapper, SqlServerErrorMapper>();
    builder.Services.AddSingleton<ISchemaMigrationsProvider, SqlServerSchemaMigrationsProvider>();
    builder.Services.AddSingleton<IRelationshipNavigationProvider, SqlServerRelationshipNavigationProvider>();
}

builder.Services.AddSingleton<IProviderFactory, MetadataProviderFactory>();
builder.Services.AddOptions<MetadataProviderFactoryOptions>()
    .Configure(options =>
    {
        if (sqlServerProviderEnabled)
        {
            options.Register(DatabaseProviderType.SqlServer, typeof(SqlServerDatabaseProvider));
        }
    });
builder.Services.AddSelectedDatabaseService();
builder.Services.AddAspireResourceDiscovery(builder.Configuration);
builder.Services.AddMetadataRefreshService();

var featureFlagBuilder = builder.Services.AddFeatureFlags()
    .AddConfigurationFeatureFlagSource()
    .AddStartupValidation();

if (sqlServerProviderEnabled)
{
    featureFlagBuilder.AddFeatureContributor<SqlServerFeatureContributor>();
}
builder.Services.AddScoped<IExplorerService, ExplorerService>();
builder.Services.AddScoped<ISchemaMigrationsService, SchemaMigrationsService>();
builder.Services.AddScoped<IRelationshipNavigatorService, RelationshipNavigatorService>();
builder.Services.AddScoped<QueryNavigationState>();
builder.Services.AddScoped<QuerySessionState>();
builder.Services.AddScoped<ExplorerNavigationState>();
builder.Services.AddScoped<FeatureFlagStateService>();
builder.Services.AddScoped<ISettingsSectionProvider, FeatureFlagsSettingsSectionProvider>();
builder.Services.AddTraceCorrelationServices();
builder.Services.AddChangeTimelineServices();
builder.Services.AddScenarioBuilderServices();
builder.Services.AddSnapshotServices();
builder.Services.AddQueryPerformanceServices();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (HasHttpsEndpoint(builder.Configuration))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool HasHttpsEndpoint(IConfiguration configuration)
{
    return !string.IsNullOrWhiteSpace(configuration["ASPNETCORE_HTTPS_PORT"])
        || !string.IsNullOrWhiteSpace(configuration["HTTPS_PORTS"]);
}
