using Oficina.Application.Budgets;
using Oficina.Application.OrderServiceHistory;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders.UseCases;

public sealed class ApproveServiceOrderUseCase(
    IServiceOrderRepository serviceOrders,
    IServiceOrderHistoryRepository history,
    IBudgetService budgets)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly IServiceOrderHistoryRepository _history = history;
    private readonly IBudgetService _budgets = budgets;

    public async Task<ServiceOrderDetailResponse> ExecuteAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await GetAwaitingApprovalServiceOrderAsync(serviceOrderId, cancellationToken);

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus(clientApproved: true);

        await _serviceOrderRepository.UpdateAsync(
            serviceOrder,
            newParts: Array.Empty<ServiceOrderPart>(),
            newWorkshopServices: Array.Empty<ServiceOrderWorkshop>(),
            cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);
        await _budgets.SetApprovalByServiceOrderAsync(serviceOrder.Id, true, cancellationToken);

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

    private async Task<ServiceOrder> GetAwaitingApprovalServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }

        if (serviceOrder.Status != ServiceOrderStatus.AwaitingApproval)
        {
            throw new InvalidOperationException(
                $"Invalid service order for this action. Current status: {serviceOrder.Status?.ToString() ?? "None"}.");
        }

        return serviceOrder;
    }
}
