#:sdk Aspire.AppHost.Sdk@13.3.0

#:package Aspire.Hosting.PostgreSQL@13.3.0

#:project ./src/CritterCab.Dispatch/CritterCab.Dispatch.csproj

var builder = DistributedApplication.CreateBuilder(args);

// === Infrastructure ===

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18-alpine")
    .WithLifetime(ContainerLifetime.Persistent);

var dispatchDb = postgres.AddDatabase("crittercab_dispatch");

// === Services ===

builder.AddProject<Projects.CritterCab_Dispatch>("dispatch")
    .WithReference(dispatchDb)
    .WaitFor(dispatchDb);

builder.Build().Run();
