using Oficina.Application.Common;
using Oficina.Domain.Customers;

namespace Oficina.Application.Vehicles.UseCases;

public class UpdateVehicleUseCase(
    IVehicleRepository vehicles)
{
    private readonly IVehicleRepository _vehicles = vehicles;

    public async Task<VehicleResponse> UpdateAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetActiveVehicleAsync(id, cancellationToken);
        var plateOwner = await _vehicles.GetByPlateAsync(Vehicle.NormalizePlate(request.Plate), cancellationToken);
        if (plateOwner is not null && plateOwner.Id != id)
        {
            throw new ConflictException("A vehicle with the informed plate already exists.");
        }

        vehicle.Update(request.Plate, request.Brand, request.Model, request.Year, request.Category);
        await _vehicles.UpdateAsync(vehicle, cancellationToken);
        return VehicleResponseMapper.Map(vehicle);
    }

    private async Task<Vehicle> GetActiveVehicleAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (vehicle is null || !vehicle.IsActive)
        {
            throw new KeyNotFoundException("Vehicle was not found.");
        }

        return vehicle;
    }
}
