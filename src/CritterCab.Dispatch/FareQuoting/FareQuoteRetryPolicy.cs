namespace CritterCab.Dispatch.FareQuoting;

// Hardcoded retry budget for the FareQuote automation. W001 §5.2 names this
// as DispatchPolicy's eventual home (Slice 11 lands DispatchPolicyConfigured);
// until then it lives as a singleton with the workshop's defaults. Tests
// override the singleton with a shorter cooldown for fast assertions.
public sealed record FareQuoteRetryPolicy(int MaxAttempts, TimeSpan Cooldown)
{
    public static readonly FareQuoteRetryPolicy Default =
        new(MaxAttempts: 3, Cooldown: TimeSpan.FromSeconds(2));
}
