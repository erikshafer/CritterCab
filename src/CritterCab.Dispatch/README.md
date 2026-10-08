# CritterCab.Dispatch

The Dispatch bounded context: a rider's ride request, from submission through fare quoting to candidate selection. Event-sourced on Marten (PostgreSQL database `crittercab_dispatch`), with one plain document fed from Telemetry over Kafka.

## Slices (feature folders)

| Folder | What it does |
|---|---|
| `RideRequesting/` | `POST /api/rides/request` (`SubmitRideRequest`) starts a `RideRequest` stream with `RideRequested`. Projections: `RequestTimeline` and `ActiveRideRequest` (inline), and `RideRequest` aggregated live. |
| `FareQuoting/` | `FareQuoteAutomation` reacts to `RideRequested` and calls `IPricingClient`, appending `FareQuoted`, or `FareQuoteFailed` once the retry budget in `FareQuoteRetryPolicy` is spent or the failure is non-transient. `FareQuoteAttempts` projects the outcome. The client is `PricingClientStub`: there is no Pricing service. |
| `CandidateSelection/` | `CandidateSelectionAutomation` reacts to `FareQuoted`, asks `INearbyAvailableDriversSource` for drivers near the pickup, and appends `CandidatesSelected` or `NoCandidatesAvailable`. `RequestRounds` projects the rounds. Limits come from `DispatchPolicySnapshot.Default`, a hardcoded policy. |
| `AvailableDrivers/` | `AvailableDriver` is a plain document, deliberately not a stream or a projection, with two independently written sides. `DriverLocationUpdatedHandler` writes the location side from Kafka; `DriverAvailabilityChanged` is a placeholder for the availability side that no service publishes and no transport carries, invoked only by tests. `NearbyAvailableDriversView` answers the candidate query with an H3 k-ring (`H3KRing`) over duplicated, indexed columns. |
| `Shared/` | `Location`, `VehicleClass`. |

## Wiring (`Program.cs`)

- **Marten** when the `crittercab_dispatch` connection string is present: mandatory stream-type declaration, inline projections, `AvailableDriver` registered with numeric revisions so its writers' `UpdateRevision(doc, existing.Version + 1)` fails on a concurrent write instead of overwriting it (stale positions are discarded separately, by comparing `ServerReceivedAt`), and Wolverine integration with fast event forwarding, which is what triggers the `*Automation` handlers. Handler discovery adds the `Automation` suffix to Wolverine's defaults.
- **Kafka** when the `kafka` connection string is present: listens on `telemetry.driver-location-updated` as consumer group `dispatch`, starting at the latest offset on a cold start, with protobuf deserialization of `DriverLocationUpdated`. Dispatch does not create the topic; Telemetry owns it.
- A `ConcurrencyException` (two writers racing on one `AvailableDriver`) retries the handler with a short cooldown, so it reloads and re-merges.
- **Without a database**, `NearbyAvailableDriversStub` stands in for the view so a bare `dotnet run` boots.
- `DriverLocationUpdated` is generated here from Telemetry's `protos/crittercab/telemetry/v1/driver_location_updated.proto` (see the comment in `CritterCab.Dispatch.csproj` on why that is not a shared assembly).

## Contracts not yet exercised

`protos/crittercab/dispatch/v1/` (`RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned`) and `protos/crittercab/pricing/v1/get_fare_quote.proto` are authored contracts that no code compiles yet.

Tests: [`tests/CritterCab.Dispatch.Tests/`](../../tests/CritterCab.Dispatch.Tests/). Design reasoning: Workshop 001 and Workshop 006 §6.5 in [`docs/workshops/`](../../docs/workshops/).
