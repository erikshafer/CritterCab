namespace CritterCab.Dispatch.FareQuoting;

public interface IPricingClient
{
    Task<GetFareQuoteResponse> GetFareQuoteAsync(
        GetFareQuoteRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record GetFareQuoteRequest(
    Guid RideRequestId,
    Location Pickup,
    Location Dropoff,
    VehicleClass VehicleClass,
    DateTimeOffset RequestedAt);

public sealed record GetFareQuoteResponse(
    long FareAmountMinorUnits,
    string Currency,
    FareBreakdown FareBreakdown,
    DateTimeOffset ValidUntil,
    string PricingPolicyVersion);
