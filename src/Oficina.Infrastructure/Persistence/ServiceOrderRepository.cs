using Microsoft.EntityFrameworkCore;
using Oficina.Application.ServiceOrders;
using Oficina.Domain.OrderService;
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
        _appDbContext.ServiceOrders
            .AsNoTracking()
            .Include(x => x.Parts)
            .Include(x => x.WorkshopServices)
            .ToListAsync(cancellationToken);


    public Task<List<ServiceOrder>> ListSchedulesAsync(CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ScheduledAt < DateTime.Now.AddDays(30))
            .ToListAsync(cancellationToken);

    public Task<List<ServiceOrder>> ListSchedulesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrders
            .AsNoTracking()
            .Where(x => x.ScheduledAt.Date == date.Date)
            .ToListAsync(cancellationToken);

    public Task<List<ServiceOrder>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        _appDbContext.ServiceOrders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync(cancellationToken);


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

    public async Task UpdateAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<ServiceOrderPart> newParts,
        IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices,
        CancellationToken cancellationToken)
    {
        foreach (var part in newParts)
        {
            _appDbContext.Add(part);
        }

        foreach (var workshopService in newWorkshopServices)
        {
            _appDbContext.Add(workshopService);
        }

        try
        {
            _appDbContext.ServiceOrders.Update(serviceOrder);
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
