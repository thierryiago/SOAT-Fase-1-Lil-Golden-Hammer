namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class ListServiceOrdersByStatusUseCase(IServiceOrderRepository serviceOrders)
{
    public async Task<IReadOnlyCollection<ServiceOrderListItemResponse>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var orders = await serviceOrders.ListByStatusAsync(cancellationToken);
        return orders.Select(ServiceOrderResponseMapper.MapListItem).ToList();
    }
}
