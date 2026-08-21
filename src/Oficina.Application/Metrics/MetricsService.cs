using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.Metrics;

public sealed class MetricsService(IWorkshopServiceExecutionTimeRepository executionTimes)
{
    private readonly IWorkshopServiceExecutionTimeRepository _executionTimes = executionTimes;

    public async Task<IReadOnlyCollection<WorkshopServiceExecutionTimeResponse>>
        GetWorkshopServiceExecutionTimesAsync(CancellationToken cancellationToken)
    {
        var data = await _executionTimes.GetAsync(cancellationToken);
        var averageTimes = CalculateAverageTimeMinutes(data.ServiceOrders);

        return data.WorkshopServices
            .Select(service => new WorkshopServiceExecutionTimeResponse(
                service.WorkshopServiceId,
                service.WorkshopServiceName,
                service.WorkshopEstimatedDurationMinutes,
                averageTimes.GetValueOrDefault(service.WorkshopServiceId)))
            .ToList();
    }

    private static IReadOnlyDictionary<Guid, decimal?> CalculateAverageTimeMinutes(
        IReadOnlyCollection<ServiceOrderExecutionTimeData> serviceOrders)
    {
        return serviceOrders
            .SelectMany(CalculateAllocatedExecutionTimes)
            .GroupBy(executionTime => executionTime.WorkshopServiceId)
            .ToDictionary(
                group => group.Key,
                group => (decimal?)group.Average(executionTime => executionTime.DurationMinutes));
    }

    private static IReadOnlyCollection<AllocatedExecutionTime> CalculateAllocatedExecutionTimes(
        ServiceOrderExecutionTimeData serviceOrder)
    {
        var executionTimeMinutes = CalculateExecutionTimeMinutes(serviceOrder.Histories);
        if (!executionTimeMinutes.HasValue)
        {
            return [];
        }

        var estimatedMinutesByWorkshopService = serviceOrder.WorkshopServices
            .GroupBy(service => service.WorkshopServiceId)
            .Select(group => new
            {
                WorkshopServiceId = group.Key,
                EstimatedDurationMinutes = group.Sum(service => service.EstimatedDurationMinutes)
            })
            .ToList();

        var totalEstimatedDurationMinutes = estimatedMinutesByWorkshopService
            .Sum(service => service.EstimatedDurationMinutes);
        if (totalEstimatedDurationMinutes <= 0)
        {
            return [];
        }

        return estimatedMinutesByWorkshopService
            .Select(service => new AllocatedExecutionTime(
                service.WorkshopServiceId,
                executionTimeMinutes.Value * service.EstimatedDurationMinutes / totalEstimatedDurationMinutes))
            .ToList();
    }

    private static decimal? CalculateExecutionTimeMinutes(
        IReadOnlyCollection<ServiceOrderStatusHistoryData> histories)
    {
        var finalized = histories
            .Where(history => history.StatusName == nameof(ServiceOrderStatus.Finalized))
            .OrderByDescending(history => history.CreatedDate)
            .FirstOrDefault();

        if (finalized is null)
        {
            return null;
        }

        var inExecution = histories
            .Where(history =>
                history.StatusName == nameof(ServiceOrderStatus.InExecution) &&
                history.CreatedDate <= finalized.CreatedDate)
            .OrderByDescending(history => history.CreatedDate)
            .FirstOrDefault();

        if (inExecution is null)
        {
            return null;
        }

        var duration = finalized.CreatedDate - inExecution.CreatedDate;
        return duration < TimeSpan.Zero ? null : (decimal)duration.TotalMinutes;
    }

    private sealed record AllocatedExecutionTime(Guid WorkshopServiceId, decimal DurationMinutes);
}
