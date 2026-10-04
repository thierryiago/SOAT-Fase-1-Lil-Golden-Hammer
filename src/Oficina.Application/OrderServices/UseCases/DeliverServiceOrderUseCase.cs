using Oficina.Application.OrderServiceHistory;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders.UseCases;

public sealed class DeliverServiceOrderUseCase(
    IServiceOrderRepository serviceOrders,
    IServiceOrderHistoryRepository history)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly IServiceOrderHistoryRepository _history = history;

    public async Task<ServiceOrderDetailResponse> ExecuteAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }

        if (serviceOrder.Status != ServiceOrderStatus.Finalized)
        {
            throw new InvalidOperationException(
                $"Invalid service order for this action. Current status: {serviceOrder.Status?.ToString() ?? "None"}.");
        }

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus(delivered: true);

        await _serviceOrderRepository.UpdateAsync(
            serviceOrder,
            Array.Empty<ServiceOrderPart>(),
            Array.Empty<ServiceOrderWorkshop>(),
            cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);

        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }

    private async Task RecordHistoryAsync(
        ServiceOrder serviceOrder,
        ServiceOrderStatus? previousStatus,
        CancellationToken cancellationToken)
    {
        if (serviceOrder.Status == previousStatus)
        {
            return;
        }

        var history = ServiceOrderHistory.Create(serviceOrder.Id, serviceOrder.Status?.ToString());
        await _history.AddAsync(history, cancellationToken);
    }
}
