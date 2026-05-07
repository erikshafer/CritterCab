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

        // Projections
        opts.Projections.LiveStreamAggregation<RideRequest>();
        opts.Projections.Add(new ActiveRequestsByRiderProjection(), ProjectionLifecycle.Inline);
        opts.Projections.Add(new RequestTimelineProjection(), ProjectionLifecycle.Inline);
    })
    .IntegrateWithWolverine()
    .UseLightweightSessions();
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

// Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Dispatch";
});

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapWolverineEndpoints();

return await app.RunJasperFxCommands(args);
