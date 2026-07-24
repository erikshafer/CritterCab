using CritterCab.Telemetry.V1;
using Wolverine.Grpc;

namespace CritterCab.Telemetry.ReportLocations;

// CritterCab's first gRPC surface in code, and its first client-streaming RPC.
//
// Deliberately empty. Grpc.Tools generates TelemetryServiceBase with a virtual ReportLocations
// taking a raw IAsyncStreamReader<LocationPing>; Wolverine's codegen then emits the override that
// adapts that reader into an IAsyncEnumerable and forwards it to
// IMessageBus.StreamAsync<LocationPing, LocationIngestAck>. The actual ingest is
// ReportLocationsHandler — a plain Wolverine handler that never sees a gRPC type.
//
// This class only declares "there is a Wolverine gRPC service behind this proto". It lives in the
// ReportLocations feature folder rather than a technical Grpc/ folder, with the handler it fronts.
//
// Client-streaming auto-codegen arrived in WolverineFx.Grpc 6.21.0; before that this shape threw
// NotSupportedException at startup and had to be hand-wired against IMessageBus. Any doc still
// describing that workaround is stale.
[WolverineGrpcService]
public abstract class TelemetryGrpcService : TelemetryService.TelemetryServiceBase;
