using Microsoft.EntityFrameworkCore;
using Oficina.Application.Metrics;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Infrastructure.Persistence;

public sealed class WorkshopServiceExecutionTimeRepository(AppDbContext appDbContext)
    : IWorkshopServiceExecutionTimeRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<IReadOnlyCollection<WorkshopServiceExecutionTimeData>> ListAsync(
        CancellationToken cancellationToken)
    {
        var services = await _appDbContext.WorkshopServices
            .AsNoTracking()
            .OrderBy(service => service.Name)
            .Select(service => new
            {
                service.Id,
                service.Name,
                service.EstimatedDurationMinutes
            })
            .ToListAsync(cancellationToken);

        var histories = await (
                from serviceOrderWorkshop in _appDbContext.ServiceOrderWorkshops.AsNoTracking()
                join serviceOrder in _appDbContext.ServiceOrders.AsNoTracking()
                    on serviceOrderWorkshop.ServiceOrderId equals serviceOrder.Id
                join history in _appDbContext.ServiceOrderHistories.AsNoTracking()
                    on serviceOrder.Id equals history.OrderServiceId
                where serviceOrder.Status == ServiceOrderStatus.Finalized &&
                      (history.StatusName == nameof(ServiceOrderStatus.InExecution) ||
                       history.StatusName == nameof(ServiceOrderStatus.Finalized))
                select new
                {
                    serviceOrderWorkshop.WorkshopServiceId,
                    history.OrderServiceId,
                    history.StatusName,
                    history.CreatedDate
                })
            .ToListAsync(cancellationToken);

        var historiesByWorkshopService = histories.ToLookup(history => history.WorkshopServiceId);

        return services
            .Select(service => new WorkshopServiceExecutionTimeData(
                service.Id,
                service.Name,
                service.EstimatedDurationMinutes,
                historiesByWorkshopService[service.Id]
                    .Select(history => new ServiceOrderStatusHistoryData(
                        history.OrderServiceId,
                        history.StatusName,
                        history.CreatedDate))
                    .ToList()))
            .ToList();
    }
}
