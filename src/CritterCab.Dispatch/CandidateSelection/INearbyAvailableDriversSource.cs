namespace CritterCab.Dispatch.CandidateSelection;

public interface INearbyAvailableDriversSource
{
    Task<IReadOnlyList<NearbyDriver>> GetDriversAsync(
        Location pickup,
        int searchRadiusMeters,
        VehicleClass vehicleClassRequired,
        CancellationToken ct = default);
}

public sealed record NearbyDriver(
    Guid DriverId,
    int DistanceMeters,
    int EtaSeconds,
    VehicleClass VehicleClass);

public sealed class NearbyAvailableDriversStub : INearbyAvailableDriversSource
{
    public IReadOnlyList<NearbyDriver> Drivers { get; set; } = [];

    public Task<IReadOnlyList<NearbyDriver>> GetDriversAsync(
        Location pickup,
        int searchRadiusMeters,
        VehicleClass vehicleClassRequired,
        CancellationToken ct = default) =>
        Task.FromResult(Drivers);
}
