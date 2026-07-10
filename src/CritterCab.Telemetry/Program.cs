using CritterCab.Telemetry.TelemetryPolicy;
using JasperFx;
using Marten;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;

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
    // ADR-011 Option A migration-time seed for the singleton policy stream.
    .InitializeWith<TelemetryPolicyBootstrap>();
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

// Enum names on the wire; Wolverine HTTP shares the Minimal-API JsonOptions this configures.
builder.Services.ConfigureSystemTextJsonForWolverineOrMinimalApi(options =>
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Telemetry";
});

var app = builder.Build();

app.MapHealthChecks("/health");

// Boundary validation: nested AbstractValidator<T> is auto-discovered; a failing rule
// short-circuits with an RFC-7807 ProblemDetails 400 before the endpoint handler runs.
app.MapWolverineEndpoints(opts => opts.UseFluentValidationProblemDetailMiddleware());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "CritterCab Telemetry API"));
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

return await app.RunJasperFxCommands(args);

public partial class Program { }
