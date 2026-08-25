namespace Oficina.Application.Metrics;

public interface IMetricExecutionTimeRepository
{
    Task<WorkshopServiceExecutionTimesData> GetAsync(
        CancellationToken cancellationToken);
}
