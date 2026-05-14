using OakIdeas.Aspire.DataExplorer.Sample.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServiceDiscovery();

builder.Services.AddHttpClient<TodoApiClient>(client =>
    client.BaseAddress = new Uri("https+http://sample-api"))
    .AddServiceDiscovery();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<OakIdeas.Aspire.DataExplorer.Sample.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
