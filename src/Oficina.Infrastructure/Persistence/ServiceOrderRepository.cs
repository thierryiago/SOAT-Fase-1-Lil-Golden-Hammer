using Microsoft.EntityFrameworkCore;
using Oficina.Application.ServiceOrders;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Infrastructure.Persistence;

public sealed class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly AppDbContext _appDbContext;

    public ServiceOrderRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrders.ToListAsync(cancellationToken);

    public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrders
            .Include(serviceOrder => serviceOrder.Parts)
            .Include(serviceOrder => serviceOrder.WorkshopServices)
            .FirstOrDefaultAsync(serviceOrder => serviceOrder.Id == id, cancellationToken);

    public async Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
    {
        await _appDbContext.ServiceOrders.AddAsync(serviceOrder, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
    {
        foreach (var part in serviceOrder.Parts)
        {
            if (_appDbContext.Entry(part).State == EntityState.Detached)
            {
                _appDbContext.Add(part);
            }
        }

        foreach (var workshopService in serviceOrder.WorkshopServices)
        {
            if (_appDbContext.Entry(workshopService).State == EntityState.Detached)
            {
                _appDbContext.Add(workshopService);
            }
        }

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict.\n{_appDbContext.ChangeTracker.DebugView.LongView}",
                ex);
        }
    }
}
