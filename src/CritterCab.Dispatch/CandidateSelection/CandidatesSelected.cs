namespace CritterCab.Dispatch.CandidateSelection;

public sealed record CandidatesSelected(
    Guid RideRequestId,
    int RoundNumber,
    IReadOnlyList<CandidateEntry> Candidates,
    SearchParameters SearchParameters,
    string DispatchPolicyVersion,
    DateTimeOffset SelectedAt) : ICandidateSelectionOutcome;

public sealed record CandidateEntry(
    Guid DriverId,
    double MatchScore,
    int DistanceMeters,
    int EtaSeconds);

public sealed record SearchParameters(
    int SearchRadiusMeters,
    int MaxCandidatesPerRound,
    VehicleClass VehicleClassRequired);
