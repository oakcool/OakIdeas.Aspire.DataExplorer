using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sample-sql", password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("sampledb");

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer()
        .AddSqlServer()
        .WithReference(sql);
}

var api = builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Api>("sample-api")
    .WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Web>("sample-web")
    .WithReference(api);

builder.Build().Run();
