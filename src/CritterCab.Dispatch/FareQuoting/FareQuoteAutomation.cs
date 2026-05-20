using System.Diagnostics;
using CritterCab.Dispatch.RideRequesting;
using Wolverine.Marten;

namespace CritterCab.Dispatch.FareQuoting;

public static class FareQuoteAutomation
{
    public static async Task<IFareQuoteOutcome> Handle(
        RideRequested @event,
        [WriteAggregate(nameof(RideRequested.RideRequestId))] RideRequest rideRequest,
        IPricingClient pricing,
        FareQuoteRetryPolicy retry,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var request = new GetFareQuoteRequest(
            RideRequestId: @event.RideRequestId,
            Pickup: @event.Pickup,
            Dropoff: @event.Dropoff,
            VehicleClass: @event.VehicleClass,
            RequestedAt: @event.RequestedAt);

        for (var attempt = 1; attempt <= retry.MaxAttempts; attempt++)
        {
            try
            {
                var response = await pricing.GetFareQuoteAsync(request, cancellationToken);
                return BuildFareQuoted(rideRequest, response, time);
            }
            catch (NonTransientPricingException ex)
            {
                return BuildFareQuoteFailed(rideRequest.Id, ex.Reason, attempt, time);
            }
            catch (TransientPricingException) when (attempt < retry.MaxAttempts)
            {
                await Task.Delay(retry.Cooldown, cancellationToken);
            }
            catch (TransientPricingException)
            {
                return BuildFareQuoteFailed(
                    rideRequest.Id,
                    FareQuoteFailureReason.PricingUnavailable,
                    attempt,
                    time);
            }
        }

        throw new UnreachableException(
            "FareQuoteAutomation retry loop must terminate via return; MaxAttempts > 0 is invariant.");
    }

    private static FareQuoted BuildFareQuoted(
        RideRequest rideRequest,
        GetFareQuoteResponse response,
        TimeProvider time) =>
        new(
            RideRequestId: rideRequest.Id,
            FareAmountMinorUnits: response.FareAmountMinorUnits,
            Currency: response.Currency,
            FareBreakdown: response.FareBreakdown,
            VehicleClass: rideRequest.VehicleClass,
            QuotedAt: time.GetUtcNow(),
            ValidUntil: response.ValidUntil,
            PricingPolicyVersion: response.PricingPolicyVersion);

    private static FareQuoteFailed BuildFareQuoteFailed(
        Guid rideRequestId,
        FareQuoteFailureReason reason,
        int attemptCount,
        TimeProvider time) =>
        new(
            RideRequestId: rideRequestId,
            Reason: reason,
            AttemptCount: attemptCount,
            FailedAt: time.GetUtcNow());
}
