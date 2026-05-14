using OakIdeas.Aspire.DataExplorer.Hosting.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDataExplorer();

var sql = builder.AddSqlServer("sample-sql")
    .AddDatabase("sampledb");

var api = builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Api>("sample-api")
    .WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Sample_Web>("sample-web")
    .WithReference(api);

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Web>("data-explorer")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
