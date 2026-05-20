using CritterCab.Dispatch.RideRequesting;
using Wolverine.Marten;

namespace CritterCab.Dispatch.FareQuoting;

public static class FareQuoteAutomation
{
    public static async Task<FareQuoted> Handle(
        RideRequested @event,
        [WriteAggregate(nameof(RideRequested.RideRequestId))] RideRequest rideRequest,
        IPricingClient pricing,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var request = new GetFareQuoteRequest(
            RideRequestId: @event.RideRequestId,
            Pickup: @event.Pickup,
            Dropoff: @event.Dropoff,
            VehicleClass: @event.VehicleClass,
            RequestedAt: @event.RequestedAt);

        var response = await pricing.GetFareQuoteAsync(request, cancellationToken);

        return new FareQuoted(
            RideRequestId: rideRequest.Id,
            FareAmountMinorUnits: response.FareAmountMinorUnits,
            Currency: response.Currency,
            FareBreakdown: response.FareBreakdown,
            VehicleClass: rideRequest.VehicleClass,
            QuotedAt: time.GetUtcNow(),
            ValidUntil: response.ValidUntil,
            PricingPolicyVersion: response.PricingPolicyVersion);
    }
}
