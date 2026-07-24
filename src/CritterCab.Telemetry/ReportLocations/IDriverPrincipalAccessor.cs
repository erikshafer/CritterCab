namespace CritterCab.Telemetry.ReportLocations;

// driverId provenance seam (W006 R5). The driver's identity comes from the authenticated
// principal, NEVER from the ping payload — which is why report_locations.proto deliberately has
// no driver_id field. Enforcement lives here and in the handler, not in the contract.
public interface IDriverPrincipalAccessor
{
    // Null when the caller presented no resolvable driver identity; the handler turns that into
    // an Unauthenticated status rather than guessing.
    Guid? CurrentDriverId { get; }
}

// Development stand-in: reads a well-known header off the ambient request. gRPC call metadata
// travels as HTTP/2 headers, so a client sending the "x-driver-id" metadata entry lands here.
//
// SWAP SITE — this is the single place that changes when the Identity BC is built. The real
// implementation reads an Entra-issued claim off HttpContext.User instead of a header; nothing
// else in Telemetry moves, because everything downstream depends only on IDriverPrincipalAccessor.
// Mirrors the ready-to-swap seam idiom used by Dispatch's PricingClientStub.
public sealed class HeaderDriverPrincipalAccessor(IHttpContextAccessor accessor)
    : IDriverPrincipalAccessor
{
    public const string DriverIdHeader = "x-driver-id";

    public Guid? CurrentDriverId
    {
        get
        {
            var header = accessor.HttpContext?.Request.Headers[DriverIdHeader];

            return Guid.TryParse(header?.ToString(), out var driverId) ? driverId : null;
        }
    }
}
