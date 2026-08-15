using Microsoft.EntityFrameworkCore;
using Oficina.Application.Vehicles;
using Oficina.Domain.Customers;

namespace Oficina.Infrastructure.Persistence;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _appDbContext;

    public VehicleRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<Vehicle>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.Vehicles.ToListAsync(cancellationToken);


    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _appDbContext.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken) =>
        _appDbContext.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Plate == plate, cancellationToken);

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await _appDbContext.Vehicles.AddAsync(vehicle, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        _appDbContext.Vehicles.Update(vehicle);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

}
