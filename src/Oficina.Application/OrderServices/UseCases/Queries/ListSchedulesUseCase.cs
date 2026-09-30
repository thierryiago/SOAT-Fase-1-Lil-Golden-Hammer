namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class ListSchedulesUseCase(IServiceOrderRepository serviceOrders)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;

    public async Task<List<ServiceOrderSchedulesDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceOrders = await _serviceOrderRepository.ListSchedulesAsync(cancellationToken);
        return serviceOrders.Select(ServiceOrderResponseMapper.MapSchedule).ToList();
    }
}
