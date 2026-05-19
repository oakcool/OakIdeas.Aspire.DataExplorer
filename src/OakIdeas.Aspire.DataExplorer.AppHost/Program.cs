using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .AddDatabase("applicationdb");

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer()
        .WithReference(sql);
}

builder.Build().Run();
