# CritterCab.Telemetry

The Telemetry bounded context: where actively pinging drivers are. A stream-processing service, not an event-sourced one. Raw GPS pings are processed in flight and never stored one by one; a driver's latest position is a document overwritten in place, and the Kafka topic is the location history. The single event-sourced stream is the throttle-policy configuration (config-as-events, [ADR-011](../../docs/decisions/011-configuration-as-events-bootstrap.md)). PostgreSQL database `crittercab_telemetry`, on Marten.

Telemetry knows nothing about availability: "active" means pinging, never available. The join of location and availability is Dispatch's (ADR-018).

## Slices (feature folders)

| Folder | What it does |
|---|---|
| `TelemetryPolicy/` | `POST /api/telemetry/policy` (`ConfigureTelemetryPolicy`, validated at the HTTP boundary with FluentValidation) appends `TelemetryPolicyConfigured` to one well-known stream; `TelemetryPolicy` is that stream aggregated live. `TelemetryPolicyBootstrap` (Marten `IInitialData`) seeds the default policy idempotently. |
| `ReportLocations/` | `TelemetryService.ReportLocations`, a gRPC client stream. `TelemetryGrpcService` is an empty `[WolverineGrpcService]` stub; Wolverine generates the service and hands the whole stream to `ReportLocationsHandler`. The handler takes the driver from the `x-driver-id` request header (`HeaderDriverPrincipalAccessor`, a development stand-in for a real identity claim), rejects pings above 100 m accuracy, computes the H3 cell (`H3CellIndexer`), and on a cell change or heartbeat publishes `DriverLocationUpdated` through `IDriverLocationPublisher` before it stores the new `LastKnownPosition`. Validation lives in the handler because Wolverine does not weave middleware for client-streaming RPCs. |
| `LastKnownPosition/` | The overwrite-in-place document. `LastKnownPositionEvictionService` (a `BackgroundService`, every 30 s) invokes `EvictStalePositions` inline, whose handler hard-deletes positions older than three heartbeat intervals. |

## Wiring (`Program.cs`)

- **Marten** when the `crittercab_telemetry` connection string is present, with the policy seed and the eviction timer registered inside the same guard.
- **gRPC** through `AddWolverineGrpc` and `MapWolverineGrpcServices`, served on the HTTPS endpoint over HTTP/2.
- **Kafka** when the `kafka` connection string is present: `DriverLocationUpdated` is published to `telemetry.driver-location-updated` inline (awaiting the broker's ack), with an idempotent producer, binary protobuf, the driver id as partition key, and the topic auto-provisioned. `UseSyncRetryBlock` makes a failed publish throw, so the position is not stored and the next ping republishes. Without a broker, `LoggingDriverLocationPublisher` logs what would have been published.
- Contracts: `protos/crittercab/telemetry/v1/report_locations.proto` (service and client generated here, so tests can drive a real stream) and `driver_location_updated.proto`.

Tests: [`tests/CritterCab.Telemetry.Tests/`](../../tests/CritterCab.Telemetry.Tests/). Design reasoning: Workshop 006 in [`docs/workshops/`](../../docs/workshops/).
