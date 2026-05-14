var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .AddDatabase("applicationdb");

builder.AddProject<Projects.OakIdeas_Aspire_DataExplorer_Web>("data-explorer")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
