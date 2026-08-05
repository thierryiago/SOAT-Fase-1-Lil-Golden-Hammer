using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Domain.Customers;

namespace Oficina.Application.Vehicles;

public sealed class VehicleService
{
    private readonly ICustomerRepository _customers;
    private readonly IVehicleRepository _vehicles;

    public VehicleService(ICustomerRepository customers, IVehicleRepository vehicles)
    {
        _customers = customers;
        _vehicles = vehicles;
    }

    public async Task<CustomerVehicleRegistrationResponse> IdentifyCustomerAndRegisterVehicleAsync(
        IdentifyCustomerAndRegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            throw new KeyNotFoundException("Customer document was not found.");
        }

        var vehicle = await CreateAsync(
            new CreateVehicleRequest(
                customer.Id,
                request.Plate,
                request.Brand,
                request.Model,
                request.Year),
            cancellationToken);

        return new CustomerVehicleRegistrationResponse(
            customer.Id,
            customer.Name,
            customer.Document,
            vehicle);
    }

    public async Task<PagedResponse<VehicleResponse>> ListAsync(
        PageRequest request,
        Guid? customerId,
        CancellationToken cancellationToken)
    {
        var vehicles = await _vehicles.ListAsync(cancellationToken);
        var search = request.Search?.Trim();
        var query = vehicles
            .Where(vehicle => vehicle.IsActive)
            .Where(vehicle => customerId is null || vehicle.CustomerId == customerId)
            .Where(vehicle =>
                string.IsNullOrWhiteSpace(search) ||
                vehicle.Plate.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                vehicle.Brand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                vehicle.Model.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(vehicle => vehicle.Plate)
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, cancellationToken);
        return vehicle is null || !vehicle.IsActive ? null : Map(vehicle);
    }

    public async Task<VehicleResponse> CreateAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        var existing = await _vehicles.GetByPlateAsync(request.Plate, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A vehicle with the informed plate already exists.");
        }

        var vehicle = Vehicle.Create(
            request.CustomerId,
            request.Plate,
            request.Brand,
            request.Model,
            request.Year);
        await _vehicles.AddAsync(vehicle, cancellationToken);
        return Map(vehicle);
    }

    public async Task<VehicleResponse> UpdateAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetActiveVehicleAsync(id, cancellationToken);
        var plateOwner = await _vehicles.GetByPlateAsync(request.Plate, cancellationToken);
        if (plateOwner is not null && plateOwner.Id != id)
        {
            throw new ConflictException("A vehicle with the informed plate already exists.");
        }

        vehicle.Update(request.Plate, request.Brand, request.Model, request.Year);
        await _vehicles.UpdateAsync(vehicle, cancellationToken);
        return Map(vehicle);
    }

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

    private async Task<Vehicle> GetActiveVehicleAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (vehicle is null || !vehicle.IsActive)
        {
            throw new KeyNotFoundException("Vehicle was not found.");
        }

        return vehicle;
    }

    private static VehicleResponse Map(Vehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.CustomerId,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year);
}
