var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("solaruydb");

var apiService = builder.AddProject<Projects.SolarUY_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.SolarUY_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
