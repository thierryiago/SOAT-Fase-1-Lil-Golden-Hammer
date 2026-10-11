namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class ListServiceOrdersUseCase(IServiceOrderRepository serviceOrders)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;

    public async Task<IReadOnlyCollection<ServiceOrderListItemResponse>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderRepository.ListAsync(cancellationToken);
        return orders.Select(ServiceOrderResponseMapper.MapListItem).ToList();
    }
}
