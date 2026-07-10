using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", secret: true);
var sqlServer = builder.AddSqlServer("sample-sql", password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
var todoDatabase = sqlServer.AddDatabase("sampledb");
var warehouseDatabase = sqlServer.AddDatabase("warehousedb");

if (builder.Environment.IsDevelopment())
{
    builder.AddDataExplorer()
        .AddSqlServer()
        .WithReference(todoDatabase)
        .WithReference(warehouseDatabase);
}

var api = builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Api>("sample-api")
    .WithReference(todoDatabase)
    .WithReference(warehouseDatabase)
    .WaitFor(todoDatabase)
    .WaitFor(warehouseDatabase);

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Web>("sample-web")
    .WithReference(api);

builder.Build().Run();
