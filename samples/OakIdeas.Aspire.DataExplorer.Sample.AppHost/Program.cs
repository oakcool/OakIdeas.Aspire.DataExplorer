using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sample-sql")
    .AddDatabase("sampledb");

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer()
        .WithReference(sql);
}

var api = builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Api>("sample-api")
    .WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Web>("sample-web")
    .WithReference(api);

builder.Build().Run();
