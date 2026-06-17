using CritterCab.Dispatch.RideRequesting;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace CritterCab.Dispatch.CandidateSelection;

public class RequestRounds
{
    public Guid Id { get; init; }
    public List<RoundEntry> Rounds { get; init; } = [];
}

public sealed record RoundEntry(
    int RoundNumber,
    string Outcome,
    string? NoCandidatesReason,
    DateTimeOffset OccurredAt);

public partial class RequestRoundsProjection : SingleStreamProjection<RequestRounds, Guid>
{
    public RequestRounds Create(IEvent<RideRequested> @event) =>
        new() { Id = @event.StreamId };

    public void Apply(IEvent<CandidatesSelected> @event, RequestRounds view) =>
        view.Rounds.Add(new RoundEntry(
            @event.Data.RoundNumber,
            nameof(CandidatesSelected),
            null,
            @event.Data.SelectedAt));

    public void Apply(IEvent<NoCandidatesAvailable> @event, RequestRounds view) =>
        view.Rounds.Add(new RoundEntry(
            @event.Data.RoundNumber,
            nameof(NoCandidatesAvailable),
            @event.Data.Reason.ToString(),
            @event.Data.SelectedAt));
}
