# CritterCab.Telemetry

The Telemetry bounded context owns the **location-of-record lifecycle for actively-pinging drivers** (W006). It is CritterCab's **fourth modeling shape — stream-processing**: its domain core is *not* event-sourced. Raw GPS pings are processed in flight (gRPC client-streaming → throttle/cell-change → Kafka publish) and never stored per-ping; the `LastKnownPosition` document is the overwrite-in-place location-of-record and the Kafka topic is the breadcrumb history.

The **only** event-sourced stream in the BC is the configuration singleton — `TelemetryPolicyConfigured` (config-as-events, [ADR-011](../../docs/decisions/011-configuration-as-events-bootstrap.md)) — which proves config-as-events is orthogonal to whether the domain core is event-sourced.

## What Telemetry owns

| Concern | Mechanism | Status |
|---|---|---|
| Throttle policy (H3 resolution, heartbeat + min-publish intervals) | `TelemetryPolicy` singleton event stream (config-as-events) | **Slice 1 — this service's first slice** |
| GPS ingest | gRPC client-streaming `ReportLocations` (`protos/crittercab/telemetry/v1/report_locations.proto`) | Slice 2 (pending) |
| `DriverLocationUpdated` publication | Kafka `telemetry.driver-location-updated` (`driver_location_updated.proto`) | Slice 3 (pending) |
| `LastKnownPosition` location-of-record | Overwrite-in-place Marten document + heartbeat-absence eviction sweep | Slice 4 (pending) |

Availability is **not** Telemetry's concern (R8): "active" means *pinging*, never *available*. The availability join lives in Dispatch (ADR-018); Telemetry only supplies location over Kafka.

## Data store

PostgreSQL database `crittercab_telemetry` (own store; never shared — ADR-002). Only the `TelemetryPolicy` singleton is event-sourced there; later slices add the `LastKnownPosition` document.

## Local dev

Runs under the Aspire AppHost (`apphost.cs`) on ports 5315 (https) / 5316 (http).
