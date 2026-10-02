namespace Oficina.Application.Vehicles.UseCases.Queries;

public class GetVehicleByIdUseCase(
    IVehicleRepository vehicles)
{
    private readonly IVehicleRepository _vehicles = vehicles;

    public async Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, cancellationToken);
        return vehicle is null || !vehicle.IsActive ? null : VehicleResponseMapper.Map(vehicle);
    }
}
