using Marten.Events.Projections;

namespace CritterCab.Dispatch.RideRequesting;

public class ActiveRideRequest
{
    public Guid Id { get; init; }
    public Guid RideRequestId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}

public partial class ActiveRequestsByRiderProjection : MultiStreamProjection<ActiveRideRequest, Guid>
{
    public ActiveRequestsByRiderProjection()
    {
        Identity<RideRequested>(e => e.RiderId);
    }

    public void Apply(ActiveRideRequest view, RideRequested @event)
    {
        view.RideRequestId = @event.RideRequestId;
        view.RequestedAt = @event.RequestedAt;
    }
}
