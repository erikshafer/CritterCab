using CritterCab.Dispatch.AvailableDrivers;
using CritterCab.Dispatch.CandidateSelection;
using CritterCab.Dispatch.FareQuoting;
using CritterCab.Dispatch.RideRequesting;
using CritterCab.Telemetry.V1;
using JasperFx;
using Marten;
using JasperFx.Events.Projections;
using Wolverine;
using Wolverine.Http;
using Wolverine.Kafka;
using Wolverine.Marten;
using Wolverine.Protobuf;

var builder = WebApplication.CreateBuilder(args);

// Marten + event sourcing
var connectionString = builder.Configuration.GetConnectionString("crittercab_dispatch");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMarten(opts =>
    {
        opts.Connection(connectionString);

        opts.AutoCreateSchemaObjects = builder.Environment.IsDevelopment()
            ? AutoCreate.CreateOrUpdate
            : AutoCreate.None;

        opts.Events.UseMandatoryStreamTypeDeclaration = true;

        // Event registration — every domain event in the Dispatch event streams.
        opts.Events.AddEventType<RideRequested>();
        opts.Events.AddEventType<FareQuoted>();
        opts.Events.AddEventType<FareQuoteFailed>();
        opts.Events.AddEventType<CandidatesSelected>();
        opts.Events.AddEventType<NoCandidatesAvailable>();

        // Projections
        opts.Projections.LiveStreamAggregation<RideRequest>();
        opts.Projections.Add(new ActiveRequestsByRiderProjection(), ProjectionLifecycle.Inline);
        opts.Projections.Add(new RequestTimelineProjection(), ProjectionLifecycle.Inline);
        opts.Projections.Add(new FareQuoteAttemptsProjection(), ProjectionLifecycle.Inline);
        opts.Projections.Add(new RequestRoundsProjection(), ProjectionLifecycle.Inline);

        // AvailableDriver is a plain document, NOT an event stream and NOT a projection — the
        // stream-processing shape crossing the BC boundary with the data (W006 §6.5). It is the
        // only non-event-sourced write path in this otherwise fully event-sourced service, so it
        // gets its schema configured here rather than registered above with the projections.
        opts.Schema.For<AvailableDriver>()
            // Duplicated COLUMNS, not computed JSONB indexes: the k-ring query filters on all three
            // together, and `= ANY(...)` over a real indexed column is the whole reason a
            // thousand-cell ring is affordable in one round trip.
            .Duplicate(x => x.H3Cell)
            .Duplicate(x => x.VehicleClass)
            .Duplicate(x => x.AvailabilityState)
            // Load-bearing, and silent if omitted. TryUpdateRevision only applies its
            // `where mt_version < ?` guard when the document is registered for numeric revisions;
            // without this it degrades to a plain unguarded upsert and stale Kafka redeliveries
            // start overwriting fresh positions with no error anywhere.
            .UseNumericRevisions(true);
    })
    .IntegrateWithWolverine(integration =>
    {
        // Forward Marten stream events to in-process Wolverine handlers
        // (e.g. RideRequested → FareQuoteAutomation per slice 5.2).
        integration.UseFastEventForwarding = true;
    })
    .UseLightweightSessions();
}

// Pricing client — stub until Pricing BC is workshopped and built.
builder.Services.AddSingleton<IPricingClient, PricingClientStub>();

// FareQuote retry budget. Hardcoded defaults per W001 §5.2 (3 attempts,
// 2-second cooldown); Slice 11's DispatchPolicyConfigured will source these
// from the DispatchPolicy projection instead.
builder.Services.AddSingleton(FareQuoteRetryPolicy.Default);

// Nearby drivers source (W006 §6.5, ADR-018) — W001 §10 parking-lot #4, closed.
//
// The real view whenever there is a document store to query; the stub only when there is not, so a
// database-less `dotnet run` still boots. The stub is DEMOTED here, not deleted: it remains the
// test double that slice 5.2 and 5.3's suites inject through the fixture, which is why those suites
// are untouched by this slice.
//
// Scoped, not singleton — it depends on IQuerySession. The stub stays a singleton because it holds
// nothing but a list. Same lifetime asymmetry, and same reason, as Telemetry's publisher seam.
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddScoped<INearbyAvailableDriversSource, NearbyAvailableDriversView>();
}
else
{
    builder.Services.AddSingleton<INearbyAvailableDriversSource, NearbyAvailableDriversStub>();
}

// One flag, read once, used by both the guard below and the listener wiring inside UseWolverine.
// Branching on the connection string twice would let the two drift into the state that breaks
// silently — a listener configured against a transport that was never registered. Same arrangement
// as Telemetry's kafkaEnabled.
var kafkaEnabled = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("kafka"));

// Dispatch policy — hardcoded defaults per W001 §5.3; Slice 11 swaps for the
// DispatchPolicyConfigured-fed projection.
builder.Services.AddSingleton(DispatchPolicySnapshot.Default);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

// Enum names on the wire restore the documented contract for every HTTP endpoint.
// Wolverine HTTP shares the Minimal-API JsonOptions this configures.
builder.Services.ConfigureSystemTextJsonForWolverineOrMinimalApi(options =>
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Dispatch";

    // Workshop §5.2 pins "Automation" as the CritterCab term for event-driven
    // handlers; expose the convention to Wolverine's handler discovery.
    //
    // This is ADDITIVE, not a replacement — Wolverine appends its built-in conventions ("Handler",
    // "Consumer", Saga, IWolverineHandler, [WolverineHandler]) at bootstrap, after this runs, and
    // the include filters are OR'd. So DriverLocationUpdatedHandler is discovered by the built-in
    // suffix without appearing here. Only DisableConventionalDiscovery() would change that.
    opts.Discovery.CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("Automation"));

    // Guarded rather than early-returned, so a broker-less run cannot silently swallow any
    // Wolverine configuration appended after this line.
    if (kafkaEnabled)
        ConfigureKafkaListening(opts);
});

// === Kafka: the slice-5 consumer (W006 §6.5) ===
//
// Dispatch's FIRST transport. Everything else this service does is in-process Marten over HTTP.
// This is also CritterCab's first cross-service flow: a ping entering Telemetry over gRPC comes out
// here as a document write.
static void ConfigureKafkaListening(WolverineOptions opts)
{
    // Read the broker address by NAME, as Telemetry does — Aspire injects it under the "kafka" key
    // via .WithReference(kafka), and the same code then works against a local container, the test
    // Testcontainer, and Azure Event Hubs with no environment branching.
    //
    // No AutoProvision() on this side. Telemetry provisions the topic because it owns it (ADR-018
    // supplier half); a consumer that auto-created the topic would mask a misconfigured topic name
    // by silently creating an empty one and then waiting forever on it.
    opts.UseKafkaUsingNamedConnection("kafka");

    // The topic name is spelled out rather than shared as a constant with Telemetry — there is no
    // shared assembly to put one in, and that is the point (ADR-019 names the convention; the
    // .proto carries it in a comment). A rename on either side must be a deliberate edit on both.
    opts.ListenToKafkaTopic("telemetry.driver-location-updated")
        // ConfigureConsumer REPLACES the parent ConsumerConfig rather than merging into it.
        // Bootstrap servers are inherited from the connection above; anything else set on the
        // parent would NOT be. There is no .GroupId(string) shortcut — this is the shape.
        //
        // The group id is the service, not the topic: every Dispatch instance shares it, so Kafka
        // distributes partitions across instances instead of delivering every position to all of
        // them. It is also what makes the redelivery-on-rebalance case real, which is precisely
        // what DriverLocationUpdatedHandler's revision guard defends against.
        //
        // Set explicitly even though Wolverine would default it to ServiceName — pinning it means a
        // future rename of the service cannot silently create a NEW consumer group, which under
        // BeginAtLatest below would start at the tail and quietly drop the group's committed
        // position. A literal is cheap insurance against an invisible reset.
        .ConfigureConsumer(c => c.GroupId = "dispatch")
        // Cold-start position, and it ONLY applies when the group has no committed offset — after
        // the first commit the group resumes where it left off and this is ignored. Stated
        // explicitly rather than left to Confluent's default, both because ConfigureConsumer above
        // REPLACES the ConsumerConfig and because the choice is a real one.
        //
        // Latest, not Earliest, on W006's own reasoning: a stale position is worthless here — §6.4
        // actively evicts positions older than three heartbeats — and the heartbeat guarantees the
        // view refills within heartbeatIntervalSeconds regardless. Earliest would replay however
        // many hours of retained telemetry the topic holds on a first deploy, discard all but the
        // newest position per driver through the revision guard, and arrive at exactly the state
        // one heartbeat would have produced in seconds.
        //
        // The cost is that a consumer which has not yet joined its group misses what is published
        // in the meantime. That is invisible in production (the heartbeat covers it) but very
        // visible in a test, which is why DispatchKafkaTestFixture performs a warm-up handshake
        // before any test produces.
        .BeginAtLatest()
        // Mandatory, and its absence fails at the FIRST MESSAGE rather than at startup.
        // ProtobufMessageSerializer.ReadFromData(byte[]) throws NotSupportedException; only the
        // (Type, Envelope) overload works. Endpoint-scoped, matching the publisher's endpoint-scoped
        // choice on the other side — Dispatch's HTTP surface stays JSON.
        .UseProtobufSerialization()
        // Hardening, not a requirement. Wolverine's default Kafka envelope mapper writes a
        // `message-type` header on publish and Telemetry uses that default, so the type would
        // resolve from the wire. Declaring it here REPLACES that header mapping with a constant,
        // which makes this listener immune to a producer that ever omits or misspells the header.
        // Safe precisely because the topic carries exactly one message type.
        .DefaultIncomingMessage<DriverLocationUpdated>();

    // Deliberately NOT setting opts.Durability.UseSyncRetryBlock. Telemetry sets it process-globally
    // to make its publish-first ordering real (W006 §6.3); it is a PRODUCER concern. A listener does
    // not inherit it across services and does not need it.
}

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapWolverineEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "CritterCab Dispatch API"));
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

return await app.RunJasperFxCommands(args);

public partial class Program { }
