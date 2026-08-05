using System.Collections.Concurrent;
using Oficina.Application.Vehicles;
using Oficina.Domain.Customers;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly ConcurrentDictionary<Guid, Vehicle> _vehicles = new();

    public Task<IReadOnlyCollection<Vehicle>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Vehicle>>(
            _vehicles.Values.OrderBy(vehicle => vehicle.Plate).ToList());

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _vehicles.TryGetValue(id, out var vehicle);
        return Task.FromResult(vehicle);
    }

    public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken)
    {
        var normalizedPlate = NormalizePlate(plate);
        var vehicle = _vehicles.Values.FirstOrDefault(existingVehicle =>
            NormalizePlate(existingVehicle.Plate) == normalizedPlate);
        return Task.FromResult(vehicle);
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        _vehicles[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        _vehicles[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }

    private static string NormalizePlate(string plate) =>
        plate.Trim().Replace("-", string.Empty).ToUpperInvariant();
}
