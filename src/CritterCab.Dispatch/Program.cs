using CritterCab.Dispatch.CandidateSelection;
using CritterCab.Dispatch.FareQuoting;
using CritterCab.Dispatch.RideRequesting;
using JasperFx;
using Marten;
using JasperFx.Events.Projections;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

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

// Nearby drivers source — stub until transport is decided (parking-lot #4).
builder.Services.AddSingleton<INearbyAvailableDriversSource, NearbyAvailableDriversStub>();

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
    opts.Discovery.CustomizeHandlerDiscovery(d => d.Includes.WithNameSuffix("Automation"));
});

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
