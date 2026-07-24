using CritterCab.Telemetry.V1;

namespace CritterCab.Telemetry.ReportLocations;

// The slice-3 Kafka publish, held behind a seam so slice 2 can be built and tested without any
// broker (W006 §6.3). The payload is the generated DriverLocationUpdated — the real published
// language shared with Dispatch (ADR-018) — so PR C swaps only the IMPLEMENTATION below for a
// WolverineFx.Kafka producer, never this contract.
public interface IDriverLocationPublisher
{
    Task PublishAsync(DriverLocationUpdated update, CancellationToken ct);
}

// PR B stand-in. Deliberately not a no-op: logging makes the publish observable end-to-end while
// the transport is absent, which is how a manual run of the ingest can be seen working before
// Kafka exists. Replaced in PR C by the real producer publishing to
// telemetry.driver-location-updated, partitioned by driverId.
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
