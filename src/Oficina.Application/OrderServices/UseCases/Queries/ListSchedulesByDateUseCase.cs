namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class ListSchedulesByDateUseCase(IServiceOrderRepository serviceOrders)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;

    public async Task<List<ServiceOrderSchedulesDto>> ExecuteAsync(
        DateTimeOffset date,
        CancellationToken cancellationToken)
    {
        var serviceOrders = await _serviceOrderRepository.ListSchedulesByDateAsync(date, cancellationToken);
        return serviceOrders.Select(ServiceOrderResponseMapper.MapSchedule).ToList();
    }
}
