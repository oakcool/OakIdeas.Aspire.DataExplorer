using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer();
}

var sql = builder.AddSqlServer("sql")
    .AddDatabase("applicationdb");

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Web>("data-explorer")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
