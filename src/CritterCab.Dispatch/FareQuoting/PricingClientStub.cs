namespace CritterCab.Dispatch.FareQuoting;

// Default response mirrors narrative 001 Moment 2: $21.50 on the first attempt,
// broken into base/distance/time, no additional fees. Tests inject a per-call
// decision delegate via the constructor to drive failure scenarios — the
// delegate receives the 1-based attempt index and can return a custom response
// or throw a Transient/NonTransientPricingException. The real PricingClient
// lands when the Pricing BC is workshopped and built.
public sealed class PricingClientStub(Func<int, GetFareQuoteResponse>? respond = null) : IPricingClient
{
    private readonly Func<int, GetFareQuoteResponse> _respond = respond ?? DefaultResponse;
    private int _attemptCount;

    public Task<GetFareQuoteResponse> GetFareQuoteAsync(
        GetFareQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var attempt = Interlocked.Increment(ref _attemptCount);
        return Task.FromResult(_respond(attempt));
    }

    private static GetFareQuoteResponse DefaultResponse(int attempt) => new(
        FareAmountMinorUnits: 2150,
        Currency: "USD",
        FareBreakdown: new FareBreakdown(
            BaseMinorUnits: 500,
            DistanceMinorUnits: 1200,
            TimeMinorUnits: 450),
        ValidUntil: DateTimeOffset.MaxValue,
        PricingPolicyVersion: "stub-v1");
}
