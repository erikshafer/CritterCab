using CritterCab.Telemetry.LastKnownPosition;
using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.TelemetryPolicy;
using CritterCab.Telemetry.V1;
using JasperFx;
using Wolverine.Grpc;
using Marten;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Kafka;
using Wolverine.Marten;
using Wolverine.Protobuf;

var builder = WebApplication.CreateBuilder(args);

// Marten + event sourcing. Telemetry is a stream-processing BC (W006 §3): the ONLY
// event-sourced stream is the config-as-events TelemetryPolicy singleton. The document
// (LastKnownPosition) and Kafka slices land later and are not event-sourced.
var connectionString = builder.Configuration.GetConnectionString("crittercab_telemetry");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMarten(opts =>
    {
        opts.Connection(connectionString);

        opts.AutoCreateSchemaObjects = builder.Environment.IsDevelopment()
            ? AutoCreate.CreateOrUpdate
            : AutoCreate.None;

        opts.Events.UseMandatoryStreamTypeDeclaration = true;

        opts.Events.AddEventType<TelemetryPolicyConfigured>();

        // TelemetryPolicy is its own live-stream aggregation (the config singleton view).
        // Self-aggregating aggregates need no `partial` (Marten 9 source-gen requires it only
        // for subclassed projections). Read live so a reconfigure is visible read-after-write.
        opts.Projections.LiveStreamAggregation<TelemetryPolicy>();
    })
    .IntegrateWithWolverine()
    .UseLightweightSessions()
    // Config-as-events bootstrap seed for the singleton policy stream: the Marten realization
    // of ADR-011 Option A (see the ADR's 2026-07-10 Amendment). Runs at the deploy-time apply
    // step and idempotently at host start. See TelemetryPolicyBootstrap.
    .InitializeWith<TelemetryPolicyBootstrap>();

    // The W006 §6.4 heartbeat-absence eviction sweep. Registered inside the Marten guard on
    // purpose: the sweep handler resolves an IDocumentSession, so without a store there is
    // nothing for it to sweep and it would only log failures every interval. Wolverine has no
    // recurring-message primitive, so the timer is a plain BackgroundService — see
    // LastKnownPositionEvictionService for why, and for why it holds no logic.
    builder.Services.AddHostedService<LastKnownPositionEvictionService>();
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

// gRPC ingest (W006 §6.2). AddWolverineGrpc registers the codegen that turns the abstract
// [WolverineGrpcService] stub into a concrete service forwarding to Wolverine handlers;
// MapWolverineGrpcServices (below) discovers and maps it.
builder.Services.AddGrpc();
builder.Services.AddWolverineGrpc();

// The ingest resolves driverId from the ambient request rather than the payload (R5), so it needs
// the accessor. HeaderDriverPrincipalAccessor is still a ready-to-swap seam — it gives way to a
// real Entra claim once Identity exists.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDriverPrincipalAccessor, HeaderDriverPrincipalAccessor>();

// The publish seam (W006 §6.3). Guarded on the connection string the same way Marten is above:
// with a broker configured the real producer is used, and without one the service still boots
// and logs what it would have published. That keeps a broker-less `dotnet run` useful and keeps
// the slice-1/2/4 test suites from having to stand up Kafka to exercise the ingest.
//
// The lifetimes differ on purpose. KafkaDriverLocationPublisher is scoped because it depends on
// IMessageBus, which Wolverine registers scoped; the logging fallback holds only an ILogger and
// stays a singleton.
// One flag, read once, used by both the registration above and the transport wiring inside
// UseWolverine below. Branching on the connection string twice would let the two drift into the
// state that breaks silently: the real publisher registered against a transport that was never
// configured, which fails at the first publish rather than at startup.
var kafkaEnabled = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("kafka"));

if (kafkaEnabled)
{
    builder.Services.AddScoped<IDriverLocationPublisher, KafkaDriverLocationPublisher>();
}
else
{
    builder.Services.AddSingleton<IDriverLocationPublisher, LoggingDriverLocationPublisher>();
}

// Enum names on the wire; Wolverine HTTP shares the Minimal-API JsonOptions this configures.
builder.Services.ConfigureSystemTextJsonForWolverineOrMinimalApi(options =>
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Telemetry";

    // Discover and register the app's FluentValidation validators into the container.
    // The HTTP UseFluentValidationProblemDetailMiddleware (below) resolves IValidator<T>
    // from DI — without this discovery step it finds no validator and the invalid command
    // passes through as 200 instead of a 400 ProblemDetails.
    opts.UseFluentValidation();

    // Guarded rather than early-returned: an early `return` here would silently swallow any
    // Wolverine configuration appended below it whenever no broker is configured.
    if (kafkaEnabled)
        ConfigureKafkaPublishing(opts);
});

// === Kafka: the slice-3 publish (W006 §6.3) ===
//
// A local function rather than an inline block, so the broker-less path is one guarded call at
// the call site instead of a branch buried in the middle of the Wolverine configuration.
static void ConfigureKafkaPublishing(WolverineOptions opts)
{
    // Read the broker address by NAME rather than by value: Aspire injects it under the "kafka"
    // key via .WithReference(kafka), and the same code then works against a local container, the
    // test Testcontainer, and Azure Event Hubs with no environment branching.
    //
    // Note UseKafkaUsingNamedConnection has a side effect beyond Kafka: it sets
    // EnableAutomaticFailureAcks = false globally, because automatic acks do not interact
    // correctly with Kafka serialization failures. That is deliberate upstream behavior — do not
    // re-enable the flag.
    //
    // AutoProvision creates the topic at startup through the Kafka admin API. It works against a
    // real broker (what Aspire and Testcontainers both start) but NOT against the Event Hubs
    // Emulator, which serves only producer and consumer APIs. An EH-Emulator environment must
    // pre-provision the topic and drop this call.
    opts.UseKafkaUsingNamedConnection("kafka")
        .AutoProvision();

    // Makes a broker rejection observable to the caller. Without this, an inline send's failure
    // is swallowed by Wolverine's default async retry block, which logs, re-posts to a background
    // block, and returns success — so ReportLocationsHandler would proceed to upsert
    // LastKnownPosition believing a publish happened. The sync block instead retries across a few
    // pauses and then rethrows, which is what makes §6.3's publish-first ordering real rather
    // than nominal.
    //
    // PROCESS-GLOBAL, not per-endpoint. Affordable today because Telemetry publishes to exactly
    // one transport; a second publisher added here inherits this and should re-weigh it.
    opts.Durability.UseSyncRetryBlock = true;

    // The topic name is spelled out rather than shared as a constant with the tests, so the
    // round-trip test asserts against the literal ADR-019 convention and would catch a rename
    // here instead of silently following it.
    opts.PublishMessage<DriverLocationUpdated>()
        .ToKafkaTopic("telemetry.driver-location-updated")
        // SendInline, not the BufferedInMemory default. Buffered batches into an in-process queue
        // and returns before the broker has seen the record, which would make "publish first,
        // then upsert" true only in statement order. Inline awaits the broker ack, so a failed
        // publish throws, the upsert is skipped, the driver's baseline stays stale, and their
        // next ping republishes — self-healing, and the branch §6.3 argued for. The cost is a
        // broker round trip per publish, which is affordable because publishes are throttled to
        // cell-change-or-heartbeat, never per raw ping.
        .SendInline()
        // enable.idempotence=true + acks=all: the broker de-duplicates producer retries, so the
        // retry block above cannot turn one position into several records.
        .UseIdempotentProducer()
        // Endpoint-scoped, so only this topic goes binary. DriverLocationUpdated is generated
        // from the .proto that IS the contract (ADR-009); serializing it as JSON would leave the
        // contract governing the type but not the wire. Telemetry's HTTP surface stays JSON —
        // the global UseProtobufSerialization overload would have taken that with it.
        .UseProtobufSerialization();
}

var app = builder.Build();

app.MapHealthChecks("/health");

// Boundary validation: nested AbstractValidator<T> is auto-discovered; a failing rule
// short-circuits with an RFC-7807 ProblemDetails 400 before the endpoint handler runs.
app.MapWolverineEndpoints(opts => opts.UseFluentValidationProblemDetailMiddleware());

// Discovers the abstract [WolverineGrpcService] stubs and maps the generated concrete services.
// Note the asymmetry with the line above: FluentValidation middleware weaves for HTTP endpoints,
// but Wolverine cannot weave Before/Validate frames for a client-streaming RPC (a before-frame
// needs a concrete request at method entry, which a stream cannot supply), so ReportLocations
// validates inside its handler instead.
app.MapWolverineGrpcServices();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "CritterCab Telemetry API"));
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

return await app.RunJasperFxCommands(args);

public partial class Program { }
