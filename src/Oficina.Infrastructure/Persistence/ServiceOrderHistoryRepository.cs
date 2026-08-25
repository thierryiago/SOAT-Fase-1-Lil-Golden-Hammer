using Microsoft.EntityFrameworkCore;
using Oficina.Application.OrderServiceHistory;
using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Infrastructure.Persistence;

public sealed class ServiceOrderHistoryRepository : IServiceOrderHistoryRepository
{
    private readonly AppDbContext _appDbContext;

    public ServiceOrderHistoryRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrderHistories.ToListAsync(cancellationToken);

    public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrderHistories
            .Where(historic => historic.OrderServiceId == serviceOrderId)
            .ToListAsync(cancellationToken);


    public async Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken)
    {
        await _appDbContext.ServiceOrderHistories.AddAsync(history, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
