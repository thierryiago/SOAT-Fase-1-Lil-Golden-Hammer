using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.Metrics;

public sealed class MetricsService(IWorkshopServiceExecutionTimeRepository executionTimes)
{
    private readonly IWorkshopServiceExecutionTimeRepository _executionTimes = executionTimes;

    public async Task<IReadOnlyCollection<WorkshopServiceExecutionTimeResponse>>
        GetWorkshopServiceExecutionTimesAsync(CancellationToken cancellationToken)
    {
        var services = await _executionTimes.ListAsync(cancellationToken);

        return services
            .Select(service => new WorkshopServiceExecutionTimeResponse(
                service.WorkshopServiceId,
                service.WorkshopServiceName,
                service.WorkshopEstimatedDurationMinutes,
                CalculateAverageTimeMinutes(service.Histories)))
            .ToList();
    }

    private static decimal? CalculateAverageTimeMinutes(
        IReadOnlyCollection<ServiceOrderStatusHistoryData> histories)
    {
        var durations = histories
            .GroupBy(history => history.ServiceOrderId)
            .Select(CalculateExecutionTimeMinutes)
            .Where(duration => duration.HasValue)
            .Select(duration => duration!.Value)
            .ToList();

        return durations.Count == 0 ? null : durations.Average();
    }

    private static decimal? CalculateExecutionTimeMinutes(
        IGrouping<Guid, ServiceOrderStatusHistoryData> histories)
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
}
