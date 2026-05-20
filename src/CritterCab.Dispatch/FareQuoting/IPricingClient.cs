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

// W001 §5.2 — curated enum on FareQuoteFailed, matches OfferDeclined pattern.
public enum FareQuoteFailureReason
{
    Unspecified,
    PricingUnavailable,
    InvalidRoute,
    NoCoverage,
    Other
}

// Transient pricing fault — FareQuoteAutomation retries within its budget.
public sealed class TransientPricingException(string message, Exception? innerException = null)
    : Exception(message, innerException);

// Non-transient pricing fault — FareQuoteAutomation emits FareQuoteFailed
// immediately with the carried reason; no further attempts.
public sealed class NonTransientPricingException(FareQuoteFailureReason reason, string message)
    : Exception(message)
{
    public FareQuoteFailureReason Reason { get; } = reason;
}
