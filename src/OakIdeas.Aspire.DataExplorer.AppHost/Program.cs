using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("applicationdb");

var dataExplorerRuntimeDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..",
    "..",
    "..",
    "..",
    "OakIdeas.Aspire.DataExplorer.Hosting",
    "DataExplorerWeb",
    "publish"));

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer(dataExplorerRuntimeDirectory)
        .WithReference(sql);
}

builder.Build().Run();
