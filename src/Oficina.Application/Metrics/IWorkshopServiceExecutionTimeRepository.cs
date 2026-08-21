namespace Oficina.Application.Metrics;

public interface IWorkshopServiceExecutionTimeRepository
{
    Task<WorkshopServiceExecutionTimesData> GetAsync(
        CancellationToken cancellationToken);
}
