using JasperFx.Events;
using Marten.Events.Aggregation;

namespace CritterCab.Dispatch.RideRequesting;

public class RequestTimeline
{
    public Guid Id { get; init; }
    public List<TimelineEntry> Entries { get; init; } = [];
}

public sealed record TimelineEntry(string EventType, DateTimeOffset OccurredAt, string Summary);

public class RequestTimelineProjection : SingleStreamProjection<RequestTimeline, Guid>
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

    private static string FormatLocation(Location loc) =>
        loc.StreetAddress ?? $"{loc.Lat:F4}, {loc.Lon:F4}";
}
