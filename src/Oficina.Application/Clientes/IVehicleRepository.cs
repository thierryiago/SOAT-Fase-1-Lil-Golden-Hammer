using Oficina.Domain.Customers;

namespace Oficina.Application.Vehicles;

public interface IVehicleRepository
{
    Task<IReadOnlyCollection<Vehicle>> ListAsync(CancellationToken cancellationToken);
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken);
}
