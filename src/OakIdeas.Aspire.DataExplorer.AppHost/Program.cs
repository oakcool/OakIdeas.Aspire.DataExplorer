using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("applicationdb");

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer()
        .WithReference(sql);
}

builder.Build().Run();
