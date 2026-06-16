using CritterCab.Dispatch.FareQuoting;
using CritterCab.Dispatch.RideRequesting;
using Wolverine.Marten;

namespace CritterCab.Dispatch.CandidateSelection;

public static class CandidateSelectionAutomation
{
    public static async Task<ICandidateSelectionOutcome> Handle(
        FareQuoted @event,
        [WriteAggregate(nameof(FareQuoted.RideRequestId))] RideRequest rideRequest,
        INearbyAvailableDriversSource nearbyDrivers,
        DispatchPolicySnapshot policy,
        TimeProvider time,
        CancellationToken ct)
    {
        var searchParams = new SearchParameters(
            SearchRadiusMeters: policy.SearchRadiusMeters,
            MaxCandidatesPerRound: policy.MaxCandidatesPerRound,
            VehicleClassRequired: @event.VehicleClass);

        var allDrivers = await nearbyDrivers.GetDriversAsync(
            rideRequest.Pickup,
            policy.SearchRadiusMeters,
            @event.VehicleClass,
            ct);

        var eligible = allDrivers
            .Where(d => d.VehicleClass == @event.VehicleClass)
            .ToList();

        var now = time.GetUtcNow();

        if (allDrivers.Count == 0)
        {
            return new NoCandidatesAvailable(
                RideRequestId: @event.RideRequestId,
                RoundNumber: 1,
                SearchParameters: searchParams,
                Reason: NoCandidatesReason.NoDriversInRange,
                SelectedAt: now);
        }

        if (eligible.Count == 0)
        {
            return new NoCandidatesAvailable(
                RideRequestId: @event.RideRequestId,
                RoundNumber: 1,
                SearchParameters: searchParams,
                Reason: NoCandidatesReason.NoCapableDriversInRange,
                SelectedAt: now);
        }

        var candidates = eligible
            .OrderBy(d => d.DistanceMeters)
            .Take(policy.MaxCandidatesPerRound)
            .Select(d => new CandidateEntry(
                DriverId: d.DriverId,
                MatchScore: 1.0 / Math.Max(1, d.DistanceMeters),
                DistanceMeters: d.DistanceMeters,
                EtaSeconds: d.EtaSeconds))
            .ToList();

        return new CandidatesSelected(
            RideRequestId: @event.RideRequestId,
            RoundNumber: 1,
            Candidates: candidates,
            SearchParameters: searchParams,
            DispatchPolicyVersion: policy.PolicyVersion,
            SelectedAt: now);
    }
}
