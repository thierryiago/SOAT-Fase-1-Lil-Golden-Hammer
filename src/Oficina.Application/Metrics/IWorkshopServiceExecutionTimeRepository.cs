namespace Oficina.Application.Metrics;

public interface IWorkshopServiceExecutionTimeRepository
{
    Task<IReadOnlyCollection<WorkshopServiceExecutionTimeData>> ListAsync(
        CancellationToken cancellationToken);
}
