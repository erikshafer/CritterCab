#:sdk Aspire.AppHost.Sdk@13.4.6

// The file-based AppHost is self-contained: it pins its Aspire versions inline
// via #:package directives. Opt out of the repo-wide Central Package Management
// (Directory.Packages.props) for the synthetic apphost.csproj — otherwise the
// inline versions collide with CPM (NU1008) and the SDK's implicit
// Aspire.Hosting.AppHost reference collides with its PackageVersion entry (NU1009).
#:property ManagePackageVersionsCentrally=false

#:package Aspire.Hosting.PostgreSQL@13.4.6
#:package Aspire.Hosting.Kafka@13.4.6

#:project ./src/CritterCab.Dispatch/CritterCab.Dispatch.csproj
#:project ./src/CritterCab.Telemetry/CritterCab.Telemetry.csproj

var builder = DistributedApplication.CreateBuilder(args);

// === Infrastructure ===

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18-alpine")
    .WithHostPort(5390)
    .WithLifetime(ContainerLifetime.Persistent);

var dispatchDb = postgres.AddDatabase("crittercab_dispatch");
var telemetryDb = postgres.AddDatabase("crittercab_telemetry");

// CritterCab's first Kafka broker (W006 §6.3). Host port 5392 is the slot the
// aspire skill's port-allocation table reserved for Kafka before any broker existed
// (5390 Postgres / 5391 SqlServer / 5392 Kafka / 5393 ASB emulator) — this claims a
// reservation rather than making a new allocation. Persistent lifetime matches the
// Postgres container so a restart of the AppHost does not discard the topic.
//
// A real Kafka container, not the Event Hubs Emulator: the emulator serves only the
// producer and consumer APIs, and Telemetry calls AutoProvision() to create the topic
// at startup, which needs the Kafka admin API. Production runs Kafka protocol against
// Azure Event Hubs with pre-provisioned topics; the service code is identical either
// way because it reads the connection string by name.
// Note the port is a constructor argument here, not a .WithHostPort(...) call — on
// Aspire 13.4.6 WithHostPort belongs to the Kafka UI container resource, not the broker.
var kafka = builder.AddKafka("kafka", port: 5392)
    .WithLifetime(ContainerLifetime.Persistent);

// === Services ===

// Pinned to CritterCab's 5300-5307 dashboard band's service slot for Dispatch
// (5310 https / 5311 http). launchProfileName: null because the service has no
// launch profile — the AppHost-declared endpoints are authoritative. gRPC rides
// the HTTPS endpoint via Kestrel HTTP/2; 5312 is reserved if a dedicated gRPC
// listener is ever needed. See docs/skills/aspire/SKILL.md § Port allocation.
//
// Dispatch LISTENS to telemetry.driver-location-updated (W006 §6.5) — its first transport, and the
// second half of CritterCab's first cross-service flow. Same named-connection arrangement as
// Telemetry below: the reference injects the broker address under the key "kafka", which is what
// UseKafkaUsingNamedConnection reads. Unlike Telemetry, Dispatch does NOT AutoProvision — the
// producer owns the topic. WaitFor is therefore ordering hygiene here rather than a hard
// requirement: a listener that starts before the broker retries, it does not fail.
builder.AddProject<Projects.CritterCab_Dispatch>("dispatch", launchProfileName: null)
    .WithHttpsEndpoint(port: 5310, name: "https")
    .WithHttpEndpoint(port: 5311, name: "http")
    .WithReference(dispatchDb)
    .WaitFor(dispatchDb)
    .WithReference(kafka)
    .WaitFor(kafka);

// Telemetry is CritterCab's second service (stream-processing shape, W006). Ports follow
// the +5 slot convention after Dispatch's 5310; 5315 https / 5316 http. See
// docs/skills/aspire/SKILL.md § Port allocation.
//
// The ReportLocations gRPC ingest (W006 §6.2) rides the HTTPS endpoint via Kestrel HTTP/2 —
// the same arrangement the Dispatch block describes above, so gRPC needs no endpoint of its
// own. This is CritterCab's first gRPC surface that actually serves traffic.
//
// Telemetry publishes DriverLocationUpdated to telemetry.driver-location-updated (W006 §6.3),
// so it is the first service to reference the broker. The reference injects the connection
// string under the name "kafka", which is exactly the key Program.cs reads via
// UseKafkaUsingNamedConnection — no address is hard-coded on either side. WaitFor matters here
// beyond ordering hygiene: startup calls AutoProvision() to create the topic.
builder.AddProject<Projects.CritterCab_Telemetry>("telemetry", launchProfileName: null)
    .WithHttpsEndpoint(port: 5315, name: "https")
    .WithHttpEndpoint(port: 5316, name: "http")
    .WithReference(telemetryDb)
    .WaitFor(telemetryDb)
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();
