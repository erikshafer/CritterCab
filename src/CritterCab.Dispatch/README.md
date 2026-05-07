# CritterCab.Dispatch

The Dispatch service owns the ride-matching lifecycle: accepting ride requests, quoting fares (via Pricing gRPC), selecting candidate drivers, fanning out offers, handling acceptance/decline/expiry, and publishing `RideAssigned` to Trips.

## Proto contract

Business-event messages published to ASB:

- `/protos/crittercab/dispatch/v1/ride_assigned.proto`
- `/protos/crittercab/dispatch/v1/ride_request_cancelled.proto`
- `/protos/crittercab/dispatch/v1/ride_request_abandoned.proto`

## Store

Marten (event sourcing) on PostgreSQL. Database: `crittercab_dispatch`.

## Transports

- **ASB** — publishes `RideAssigned`, `RideRequestCancelled`, `RideRequestAbandoned` business events.
- **gRPC** — exposes `DispatchService` for BFF consumption (future).

## Cross-service dependencies

- **Pricing** — `GetFareQuote` gRPC unary call during the fare-quoting slice.
- **Trips** — consumes `RideAssigned` via ASB topic subscription.
