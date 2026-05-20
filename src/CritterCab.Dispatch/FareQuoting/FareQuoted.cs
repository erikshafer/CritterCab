namespace CritterCab.Dispatch.FareQuoting;

public sealed record FareQuoted(
    Guid RideRequestId,
    long FareAmountMinorUnits,
    string Currency,
    FareBreakdown FareBreakdown,
    VehicleClass VehicleClass,
    DateTimeOffset QuotedAt,
    DateTimeOffset ValidUntil,
    string PricingPolicyVersion) : IFareQuoteOutcome;

public sealed record FareBreakdown(
    long BaseMinorUnits,
    long DistanceMinorUnits,
    long TimeMinorUnits,
    long? FeesMinorUnits = null);
