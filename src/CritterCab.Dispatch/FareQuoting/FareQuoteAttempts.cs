using CritterCab.Dispatch.RideRequesting;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace CritterCab.Dispatch.FareQuoting;

// Terminal-events-only projection — W001 §5.2 says retry attempts themselves
// are NOT emitted as events, so the view captures only the final attemptCount
// and outcome (per the workshop-inconsistency resolution committed in
// prompt 004's `## Framing`). In-flight retry budgeting lives in the handler's
// loop variable, not here.
public class FareQuoteAttempts
{
    public Guid Id { get; init; }
    public int AttemptCount { get; init; }
    public FareQuoteOutcome Outcome { get; init; }
    public FareQuoteFailureReason? FailureReason { get; init; }
    public DateTimeOffset SettledAt { get; init; }
}

public enum FareQuoteOutcome { Pending, Quoted, Failed }

public class FareQuoteAttemptsProjection : SingleStreamProjection<FareQuoteAttempts, Guid>
{
    public FareQuoteAttempts Create(IEvent<RideRequested> e) => new()
    {
        Id = e.StreamId,
        AttemptCount = 0,
        Outcome = FareQuoteOutcome.Pending,
        SettledAt = e.Data.RequestedAt
    };

    public FareQuoteAttempts Apply(IEvent<FareQuoted> e, FareQuoteAttempts view) =>
        new()
        {
            Id = view.Id,
            AttemptCount = 1,
            Outcome = FareQuoteOutcome.Quoted,
            FailureReason = null,
            SettledAt = e.Data.QuotedAt
        };

    public FareQuoteAttempts Apply(IEvent<FareQuoteFailed> e, FareQuoteAttempts view) =>
        new()
        {
            Id = view.Id,
            AttemptCount = e.Data.AttemptCount,
            Outcome = FareQuoteOutcome.Failed,
            FailureReason = e.Data.Reason,
            SettledAt = e.Data.FailedAt
        };
}
