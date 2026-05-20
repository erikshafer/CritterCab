namespace CritterCab.Dispatch.FareQuoting;

// Mirrors narrative 001 Moment 2: Pricing answers on the first attempt with
// $21.50, broken into base/distance/time, no additional fees. The real
// PricingClient lands when the Pricing BC is workshopped and built.
public sealed class PricingClientStub : IPricingClient
{
    public Task<GetFareQuoteResponse> GetFareQuoteAsync(
        GetFareQuoteRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new GetFareQuoteResponse(
            FareAmountMinorUnits: 2150,
            Currency: "USD",
            FareBreakdown: new FareBreakdown(
                BaseMinorUnits: 500,
                DistanceMinorUnits: 1200,
                TimeMinorUnits: 450),
            ValidUntil: DateTimeOffset.MaxValue,
            PricingPolicyVersion: "stub-v1"));
}
