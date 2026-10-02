using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Notifications;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public sealed class ServiceOrderService(
    IServiceOrderRepository serviceOrders,
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    IStockRepository stocks,
    IServiceOrderHistoryRepository history,
    IBudgetService budgets,
    NotificationService notifications)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly ICustomerRepository _customerRepository = customers;
    private readonly IVehicleRepository _vehicleRepository = vehicles;
    private readonly IStockRepository _stocks = stocks;
    private readonly IServiceOrderHistoryRepository _history = history;
    private readonly IBudgetService _budgets = budgets;
    private readonly NotificationService _notifications = notifications;

    public async Task<IReadOnlyCollection<ServiceOrderListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderRepository.ListAsync(cancellationToken);
        return orders.Select(MapListItem).ToList();
    }

    public async Task<ServiceOrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : ServiceOrderResponseMapper.MapDetail(order);
    }

    public async Task<ServiceOrderTrackingResponse?> TrackAsync(Guid serviceOrderId, string document, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
        if (customer is null || customer.Document != Customer.NormalizeDocument(document))
        {
            return null;
        }

        var history = await _history.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
        var timeline = history
            .OrderByDescending(entry => entry.CreatedDate)
            .Select(entry => new ServiceOrderTrackingHistoryItem(entry.StatusName, entry.CreatedDate))
            .ToList();

        return new ServiceOrderTrackingResponse(order.Id, order.Status?.ToString(), order.Description, order.CreatedAt, timeline);
    }

    public async Task<IReadOnlyCollection<ServiceOrderTrackingSummaryResponse>> TrackByDocumentAsync(string document, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByDocumentAsync(Customer.NormalizeDocument(document), cancellationToken);
        if (customer is null)
        {
            return [];
        }

        var orders = await _serviceOrderRepository.ListByCustomerAsync(customer.Id, cancellationToken);
        return [.. orders
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new ServiceOrderTrackingSummaryResponse(order.Id, order.Status?.ToString(), order.Description, order.CreatedAt))];
    }

    public async Task<ServiceOrderDetailResponse> ApproveAsync(Guid serviceOrderId, CancellationToken cancellationToken)
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

    public async Task<ServiceOrderDetailResponse> CancelAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await GetAwaitingApprovalServiceOrderAsync(serviceOrderId, cancellationToken);

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus(clientApproved: false);

        await ReturnPartsToStockAsync(serviceOrder.Parts, cancellationToken);

        await _serviceOrderRepository.UpdateAsync(
            serviceOrder,
            Array.Empty<ServiceOrderPart>(),
            Array.Empty<ServiceOrderWorkshop>(),
            cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);
        await _budgets.SetApprovalByServiceOrderAsync(serviceOrder.Id, false, cancellationToken);

        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }

    public async Task<ServiceOrderDetailResponse> FinalizeAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }

        if (serviceOrder.Status != ServiceOrderStatus.InExecution)
        {
            throw new InvalidOperationException(
                $"Invalid service order for this action. Current status: {serviceOrder.Status?.ToString() ?? "None"}.");
        }

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus(finalized: true);

        await _serviceOrderRepository.UpdateAsync(
            serviceOrder,
            Array.Empty<ServiceOrderPart>(),
            Array.Empty<ServiceOrderWorkshop>(),
            cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(serviceOrder.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer was not found.");
        var vehicle = serviceOrder.VehicleId is { } vehicleId
            ? await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken)
            : null;
        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle was not found.");
        }

        await _notifications.SendVehicleReadyForPickupAsync(
            customer.Name,
            customer.Email,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            cancellationToken);

        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }

    public async Task<ServiceOrderDetailResponse> DeliverAsync(Guid serviceOrderId, CancellationToken cancellationToken)
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

    private async Task ReturnPartsToStockAsync(
        IReadOnlyCollection<ServiceOrderPart> parts,
        CancellationToken cancellationToken)
    {
        foreach (var part in parts)
        {
            var stock = await _stocks.GetByPartIdAsync(part.PartId, cancellationToken);
            if (stock is null)
            {
                continue;
            }

            stock.Release(part.QuantityUsed);
            await _stocks.UpdateAsync(stock, cancellationToken);
        }
    }

    public async Task<List<ServiceOrderSchedulesDto>> ListSchedulesAsync()
    {
        var serviceOrders = await _serviceOrderRepository.ListSchedulesAsync(CancellationToken.None);
        if (serviceOrders.Count != 0)
        {
            var scheduleList = serviceOrders.Select(so => new ServiceOrderSchedulesDto
            {
                OrderServiceId = so.Id,
                ScheduleDate = TimeZoneInfo.ConvertTimeFromUtc(so.ScheduledAt.DateTime, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"))
            }).ToList();

            return scheduleList;
        }

        return [];
    }

    public async Task<List<ServiceOrderSchedulesDto>> ListSchedulesByDateAsync(DateTime date)
    {
        var serviceOrders = await _serviceOrderRepository.ListSchedulesByDateAsync(date, CancellationToken.None);
        if (serviceOrders.Count != 0)
        {

            var scheduleList = serviceOrders.Select(so => new ServiceOrderSchedulesDto
            {
                OrderServiceId = so.Id,
                ScheduleDate = TimeZoneInfo.ConvertTimeFromUtc(so.ScheduledAt.DateTime, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"))
            }).ToList();

            return scheduleList;
        }

        return [];
    }

    private static ServiceOrderListItemResponse MapListItem(ServiceOrder order) =>
        new(order.Id, order.CustomerId, order.VehicleId, order.MechanicId, order.Description,
            order.Status, order.CreatedAt, order.TotalParts);
}
