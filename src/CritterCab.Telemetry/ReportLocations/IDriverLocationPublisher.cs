using CritterCab.Telemetry.V1;

namespace CritterCab.Telemetry.ReportLocations;

// The slice-3 Kafka publish, held behind a seam so the ingest can be built and tested without any
// broker (W006 §6.3). The payload is the generated DriverLocationUpdated — the real published
// language shared with Dispatch (ADR-018) — which is what let PR C swap in the real producer
// (KafkaDriverLocationPublisher) without touching this contract or its caller.
//
// The seam earned its keep twice over: the ingest's own tests still run against a recording
// implementation with no broker in sight, and LoggingDriverLocationPublisher below remains the
// no-broker fallback.
public interface IDriverLocationPublisher
{
    Task PublishAsync(DriverLocationUpdated update, CancellationToken ct);
}

// The no-broker fallback, registered when no Kafka connection string is configured. Originally
// PR B's stand-in for the absent transport; it survives PR C because a broker-less `dotnet run`
// is still worth having, and logging keeps the publish decision observable when there is nothing
// to publish to. Deliberately not a no-op, for that reason.
public sealed class LoggingDriverLocationPublisher(ILogger<LoggingDriverLocationPublisher> logger)
    : IDriverLocationPublisher
{
    public Task PublishAsync(DriverLocationUpdated update, CancellationToken ct)
    {
        logger.LogInformation(
            "DriverLocationUpdated (not yet transported): driver {DriverId} at cell {H3Cell} " +
            "resolution {H3Resolution}, policy version {ThrottlePolicyVersion}.",
            update.DriverId,
            update.H3Cell,
            update.H3Resolution,
            update.ThrottlePolicyVersion);

        return Task.CompletedTask;
    }
}
