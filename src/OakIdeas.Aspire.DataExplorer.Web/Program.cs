using OakIdeas.Aspire.DataExplorer.Core.Guards;
using OakIdeas.Aspire.DataExplorer.Web.Components;

var builder = WebApplication.CreateBuilder(args);

DevelopmentEnvironmentGuard.EnsureDevelopment(
    builder.Environment.IsDevelopment(),
    "OakIdeas.Aspire.DataExplorer is a development-time-only tool and cannot run outside Development.");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
