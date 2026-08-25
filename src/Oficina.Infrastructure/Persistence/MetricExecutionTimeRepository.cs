using Microsoft.EntityFrameworkCore;
using Oficina.Application.Metrics;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Infrastructure.Persistence;

public sealed class MetricExecutionTimeRepository(AppDbContext appDbContext)
    : IMetricExecutionTimeRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<WorkshopServiceExecutionTimesData> GetAsync(
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

        var finalizedServiceOrderIds = await _appDbContext.ServiceOrders
            .AsNoTracking()
            .Where(serviceOrder => serviceOrder.Status == ServiceOrderStatus.Finalized)
            .Select(serviceOrder => serviceOrder.Id)
            .ToListAsync(cancellationToken);

        var workshopServicesByServiceOrder = await (
                from serviceOrderWorkshop in _appDbContext.ServiceOrderWorkshops.AsNoTracking()
                join workshopService in _appDbContext.WorkshopServices.AsNoTracking()
                    on serviceOrderWorkshop.WorkshopServiceId equals workshopService.Id
                where finalizedServiceOrderIds.Contains(serviceOrderWorkshop.ServiceOrderId)
                select new
                {
                    serviceOrderWorkshop.ServiceOrderId,
                    serviceOrderWorkshop.WorkshopServiceId,
                    workshopService.EstimatedDurationMinutes
                })
            .ToListAsync(cancellationToken);

        var histories = await _appDbContext.ServiceOrderHistories
            .AsNoTracking()
            .Where(history =>
                finalizedServiceOrderIds.Contains(history.OrderServiceId) &&
                (history.StatusName == nameof(ServiceOrderStatus.InExecution) ||
                 history.StatusName == nameof(ServiceOrderStatus.Finalized)))
            .Select(history => new
            {
                history.OrderServiceId,
                history.StatusName,
                history.CreatedDate
            })
            .ToListAsync(cancellationToken);

        var workshopServicesLookup = workshopServicesByServiceOrder
            .ToLookup(service => service.ServiceOrderId);
        var historiesLookup = histories.ToLookup(history => history.OrderServiceId);

        return new WorkshopServiceExecutionTimesData(
            services
                .Select(service => new WorkshopServiceExecutionTimeData(
                    service.Id,
                    service.Name,
                    service.EstimatedDurationMinutes))
                .ToList(),
            finalizedServiceOrderIds
                .Select(serviceOrderId => new ServiceOrderExecutionTimeData(
                    serviceOrderId,
                    workshopServicesLookup[serviceOrderId]
                        .Select(service => new ServiceOrderWorkshopServiceData(
                            service.WorkshopServiceId,
                            service.EstimatedDurationMinutes))
                        .ToList(),
                    historiesLookup[serviceOrderId]
                    .Select(history => new ServiceOrderStatusHistoryData(
                        history.OrderServiceId,
                        history.StatusName,
                        history.CreatedDate))
                    .ToList()))
                .ToList());
    }
}
