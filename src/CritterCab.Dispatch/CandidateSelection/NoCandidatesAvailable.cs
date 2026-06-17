namespace CritterCab.Dispatch.CandidateSelection;

public sealed record NoCandidatesAvailable(
    Guid RideRequestId,
    int RoundNumber,
    SearchParameters SearchParameters,
    NoCandidatesReason Reason,
    DateTimeOffset SelectedAt) : ICandidateSelectionOutcome;

public enum NoCandidatesReason
{
    NoDriversInRange,
    NoCapableDriversInRange,
    AllCapableDriversOccupied,
    Other
}
