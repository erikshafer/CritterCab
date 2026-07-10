#:sdk Aspire.AppHost.Sdk@13.4.3

// The file-based AppHost is self-contained: it pins its Aspire versions inline
// via #:package directives. Opt out of the repo-wide Central Package Management
// (Directory.Packages.props) for the synthetic apphost.csproj — otherwise the
// inline versions collide with CPM (NU1008) and the SDK's implicit
// Aspire.Hosting.AppHost reference collides with its PackageVersion entry (NU1009).
#:property ManagePackageVersionsCentrally=false

#:package Aspire.Hosting.PostgreSQL@13.4.3

#:project ./src/CritterCab.Dispatch/CritterCab.Dispatch.csproj

var builder = DistributedApplication.CreateBuilder(args);

// === Infrastructure ===

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18-alpine")
    .WithHostPort(5390)
    .WithLifetime(ContainerLifetime.Persistent);

var dispatchDb = postgres.AddDatabase("crittercab_dispatch");
var telemetryDb = postgres.AddDatabase("crittercab_telemetry");

// === Services ===

// Pinned to CritterCab's 5300-5307 dashboard band's service slot for Dispatch
// (5310 https / 5311 http). launchProfileName: null because the service has no
// launch profile — the AppHost-declared endpoints are authoritative. gRPC rides
// the HTTPS endpoint via Kestrel HTTP/2; 5312 is reserved if a dedicated gRPC
// listener is ever needed. See docs/skills/aspire/SKILL.md § Port allocation.
builder.AddProject<Projects.CritterCab_Dispatch>("dispatch", launchProfileName: null)
    .WithHttpsEndpoint(port: 5310, name: "https")
    .WithHttpEndpoint(port: 5311, name: "http")
    .WithReference(dispatchDb)
    .WaitFor(dispatchDb);

// Telemetry is CritterCab's second service (stream-processing shape, W006). This PR
// stands up its skeleton + config-as-events slice 1 only; the Kafka resource that
// slice 3 needs is deliberately NOT wired yet (same deferral the Dispatch skeleton
// made for transport). Ports follow the +5 slot convention after Dispatch's 5310;
// 5315 https / 5316 http. See docs/skills/aspire/SKILL.md § Port allocation.
builder.AddProject<Projects.CritterCab_Telemetry>("telemetry", launchProfileName: null)
    .WithHttpsEndpoint(port: 5315, name: "https")
    .WithHttpEndpoint(port: 5316, name: "http")
    .WithReference(telemetryDb)
    .WaitFor(telemetryDb);

builder.Build().Run();
