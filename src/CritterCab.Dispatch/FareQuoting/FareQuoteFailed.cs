namespace CritterCab.Dispatch.FareQuoting;

// W001 §5.2 — terminal for the fare-quote step (not terminal for the Ride Request).
// Consumed by Slice 9's re-dispatch/abandonment automation. Dispatch-local; not
// emitted on the cross-BC integration surface.
public sealed record FareQuoteFailed(
    Guid RideRequestId,
    FareQuoteFailureReason Reason,
    int AttemptCount,
    DateTimeOffset FailedAt) : IFareQuoteOutcome;
