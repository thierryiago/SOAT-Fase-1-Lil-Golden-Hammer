using Oficina.Domain.Customers;
using Oficina.Domain.Vehicles;

namespace Oficina.Application.Vehicles;

public interface IVehicleRepository
{
    Task<List<Vehicle>> ListAsync(CancellationToken cancellationToken);
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken);
}
