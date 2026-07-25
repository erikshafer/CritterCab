using CritterCab.Telemetry.ReportLocations;
using CritterCab.Telemetry.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace CritterCab.Telemetry.Tests.ReportLocations;

// Drives a real gRPC client stream against a test host. Extracted once slice 3 added a second
// fixture and a third test class — the open-stream / write-pings / half-close / await-ack dance
// is identical everywhere, and the only thing that ever varies is which host's channel to use.
//
// Deliberately a static helper over a channel factory rather than a base class: the two fixtures
// have nothing else in common (one runs a broker, the other swaps the publisher for a recorder),
// so inheriting from a shared test base would couple them for the sake of four lines.
internal static class ReportLocationsClient
{
    public static async Task<LocationIngestAck> StreamAsync(
        Func<GrpcChannel> channelFactory,
        Guid driverId,
        params LocationPing[] pings)
    {
        using var channel = channelFactory();
        var client = new TelemetryService.TelemetryServiceClient(channel);

        // gRPC call metadata travels as HTTP/2 headers, which is how the dev principal accessor
        // sees it. The real Entra claim replaces this without the handler changing.
        using var call = client.ReportLocations(new Metadata
        {
            { HeaderDriverPrincipalAccessor.DriverIdHeader, driverId.ToString() }
        });

        foreach (var ping in pings)
            await call.RequestStream.WriteAsync(ping);

        // Half-close: this is what makes the single ack come back.
        await call.RequestStream.CompleteAsync();

        return await call.ResponseAsync;
    }

    // Opens a stream carrying no driver identity at all, for the R5 rejection case.
    public static async Task<LocationIngestAck> StreamWithoutIdentityAsync(
        Func<GrpcChannel> channelFactory,
        params LocationPing[] pings)
    {
        using var channel = channelFactory();
        var client = new TelemetryService.TelemetryServiceClient(channel);
        using var call = client.ReportLocations();

        foreach (var ping in pings)
            await call.RequestStream.WriteAsync(ping);

        await call.RequestStream.CompleteAsync();

        return await call.ResponseAsync;
    }

    public static LocationPing PingAt(double lat, double lon, double accuracyMeters = 8d) => new()
    {
        Lat = lat,
        Lon = lon,
        AccuracyMeters = accuracyMeters,
        DeviceTimestamp = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
    };
}
