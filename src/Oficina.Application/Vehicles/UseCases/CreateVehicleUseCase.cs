using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Domain.Customers;

namespace Oficina.Application.Vehicles.UseCases;

public class CreateVehicleUseCase(
    ICustomerRepository customers,
    IVehicleRepository vehicles)
{
    private readonly ICustomerRepository _customers = customers;
    private readonly IVehicleRepository _vehicles = vehicles;

    public async Task<VehicleResponse> CreateAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        var existing = await _vehicles.GetByPlateAsync(Vehicle.NormalizePlate(request.Plate), cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A vehicle with the informed plate already exists.");
        }

        var vehicle = Vehicle.Create(
            request.CustomerId,
            request.Plate,
            request.Brand,
            request.Model,
            request.Year,
            request.Category);
        await _vehicles.AddAsync(vehicle, cancellationToken);
        return VehicleResponseMapper.Map(vehicle);
    }
}
