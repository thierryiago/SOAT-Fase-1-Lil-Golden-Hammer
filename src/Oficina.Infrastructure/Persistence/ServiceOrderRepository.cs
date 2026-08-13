using Microsoft.EntityFrameworkCore;
using Oficina.Application.ServiceOrders;
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
        _appDbContext.ServiceOrders.FirstOrDefaultAsync(serviceOrder => serviceOrder.Id == id, cancellationToken);

    public async Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
    {
        await _appDbContext.ServiceOrders.AddAsync(serviceOrder, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
    {
        _appDbContext.ServiceOrders.Update(serviceOrder);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
