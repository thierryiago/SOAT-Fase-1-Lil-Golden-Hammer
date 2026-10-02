namespace Oficina.Application.Vehicles.UseCases;

public class DeleteVehicleUseCase(
    IVehicleRepository vehicles)
{
    private readonly IVehicleRepository _vehicles = vehicles;

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (vehicle is null || !vehicle.IsActive)
        {
            return false;
        }

        vehicle.Deactivate();
        await _vehicles.UpdateAsync(vehicle, cancellationToken);
        return true;
    }
}
