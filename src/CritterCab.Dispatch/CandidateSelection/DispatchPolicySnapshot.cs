namespace CritterCab.Dispatch.CandidateSelection;

public sealed record DispatchPolicySnapshot(
    int SearchRadiusMeters,
    int MaxCandidatesPerRound,
    string PolicyVersion)
{
    public static readonly DispatchPolicySnapshot Default =
        new(SearchRadiusMeters: 5000, MaxCandidatesPerRound: 5, PolicyVersion: "default-v1");
}
