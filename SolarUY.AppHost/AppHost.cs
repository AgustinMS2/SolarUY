var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("solaruydb");

// Fixed port so the token issuer (https://localhost:8080/realms/SolarUY) stays stable.
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithRealmImport("./Realms");

var apiService = builder.AddProject<Projects.SolarUY_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgres)
    .WithReference(keycloak)
    .WaitFor(postgres)
    .WaitFor(keycloak);

builder.AddProject<Projects.SolarUY_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(keycloak)
    .WaitFor(apiService)
    .WaitFor(keycloak);

builder.Build().Run();
