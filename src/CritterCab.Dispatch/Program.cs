using JasperFx;
using Marten;
using Wolverine;
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
    })
    .IntegrateWithWolverine()
    .UseLightweightSessions();
}

builder.Services.AddHealthChecks();

// Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "Dispatch";
});

var app = builder.Build();

app.MapHealthChecks("/health");

return await app.RunJasperFxCommands(args);
