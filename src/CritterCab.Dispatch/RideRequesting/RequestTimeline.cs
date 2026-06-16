using CritterCab.Dispatch.CandidateSelection;
using CritterCab.Dispatch.FareQuoting;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace CritterCab.Dispatch.RideRequesting;

public class RequestTimeline
{
    public Guid Id { get; init; }
    public List<TimelineEntry> Entries { get; init; } = [];
}

public sealed record TimelineEntry(string EventType, DateTimeOffset OccurredAt, string Summary);

public partial class RequestTimelineProjection : SingleStreamProjection<RequestTimeline, Guid>
{
    public RequestTimeline Create(IEvent<RideRequested> @event) =>
        new()
        {
            Id = @event.StreamId,
            Entries =
            [
                new TimelineEntry(
                    nameof(RideRequested),
                    @event.Data.RequestedAt,
                    $"Ride requested: {FormatLocation(@event.Data.Pickup)} → {FormatLocation(@event.Data.Dropoff)}")
            ]
        };

    public void Apply(IEvent<FareQuoted> @event, RequestTimeline view) =>
        view.Entries.Add(new TimelineEntry(
            nameof(FareQuoted),
            @event.Data.QuotedAt,
            $"Fare quoted: ${@event.Data.FareAmountMinorUnits / 100m:F2}"));

    public void Apply(IEvent<FareQuoteFailed> @event, RequestTimeline view) =>
        view.Entries.Add(new TimelineEntry(
            nameof(FareQuoteFailed),
            @event.Data.FailedAt,
            $"Fare quote failed: {@event.Data.Reason} after {@event.Data.AttemptCount} attempt(s)"));

    public void Apply(IEvent<CandidatesSelected> @event, RequestTimeline view) =>
        view.Entries.Add(new TimelineEntry(
            nameof(CandidatesSelected),
            @event.Data.SelectedAt,
            $"Candidates selected: {@event.Data.Candidates.Count} driver(s) for round {@event.Data.RoundNumber}"));

    public void Apply(IEvent<NoCandidatesAvailable> @event, RequestTimeline view) =>
        view.Entries.Add(new TimelineEntry(
            nameof(NoCandidatesAvailable),
            @event.Data.SelectedAt,
            $"No candidates available: {@event.Data.Reason} (round {@event.Data.RoundNumber})"));

    private static string FormatLocation(Location loc) =>
        loc.StreetAddress ?? $"{loc.Lat:F4}, {loc.Lon:F4}";
}
