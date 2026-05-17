using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using OakIdeas.Aspire.DataExplorer.SqlServer.Diagnostics;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components;
using OakIdeas.Aspire.DataExplorer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

DevelopmentEnvironmentGuard.EnsureDevelopment(
    builder.Environment.IsDevelopment(),
    "OakIdeas.Aspire.DataExplorer is a development-time-only tool and cannot run outside Development.");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<SqlServerDatabaseProvider>();
builder.Services.AddSingleton<IProviderErrorMapper, SqlServerErrorMapper>();
builder.Services.AddSingleton<IProviderFactory, MetadataProviderFactory>();
builder.Services.AddOptions<MetadataProviderFactoryOptions>()
    .Configure(options => options.Register(DatabaseProviderType.SqlServer, typeof(SqlServerDatabaseProvider)));
builder.Services.AddSelectedDatabaseService();
builder.Services.AddAspireResourceDiscovery(builder.Configuration);
builder.Services.AddMetadataRefreshService();
builder.Services.AddScoped<IExplorerService, ExplorerService>();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
