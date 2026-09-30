namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class GetServiceOrderByIdUseCase(IServiceOrderRepository serviceOrders)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;

    public async Task<ServiceOrderDetailResponse?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : ServiceOrderResponseMapper.MapDetail(order);
    }
}
