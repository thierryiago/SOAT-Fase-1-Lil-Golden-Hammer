using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Notifications;
using Oficina.Application.OrdensServico;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Parts;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Stock;

namespace Oficina.Application.ServiceOrders;

public sealed class ServiceOrderService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IPartRepository _parts;
    private readonly IWorkshopServiceRepository _workshopServices;
    private readonly IStockRepository _stocks;
    private readonly IServiceOrderHistoryRepository _history;
    private readonly IBudgetService _budgets;
    private readonly NotificationService _notifications;

    public ServiceOrderService(
        IServiceOrderRepository serviceOrders,
        ICustomerRepository customers,
        IVehicleRepository vehicles,
        IPartRepository parts,
        IWorkshopServiceRepository workshopServices,
        IStockRepository stocks,
        IServiceOrderHistoryRepository history,
        IBudgetService budgets,
        NotificationService notifications)
    {
        _serviceOrderRepository = serviceOrders;
        _customerRepository = customers;
        _vehicleRepository = vehicles;
        _parts = parts;
        _workshopServices = workshopServices;
        _stocks = stocks;
        _history = history;
        _budgets = budgets;
        _notifications = notifications;
    }

    public async Task<IReadOnlyCollection<ServiceOrderListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderRepository.ListAsync(cancellationToken);
        return orders.Select(MapListItem).ToList();
    }

    public async Task<ServiceOrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : MapDetail(order);
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

    public async Task<ServiceOrderDetailResponse> OpenAsync(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new InvalidOperationException("Customer was not found.");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
            throw new InvalidOperationException("Vehicle was not found.");

        var serviceOrder = ServiceOrder.Open(request.CustomerId, request.VehicleId, request.Description);
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        return MapDetail(serviceOrder);
    }

    public async Task<ServiceOrderDetailResponse> UpdateAsync(UpdateServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(request.ServiceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }
        var hasNewItems = HasNewItems(serviceOrder, request);
        serviceOrder.ValidateUpdate(request.MechanicId, hasNewItems);

        IReadOnlyCollection<ServiceOrderPart>? parts = null;
        IReadOnlyCollection<ServiceOrderPart> newParts = Array.Empty<ServiceOrderPart>();
        if (request.Parts is not null)
        {
            (parts, newParts) = await ResolvePartsAsync(serviceOrder, request.Parts, cancellationToken);
        }

        IReadOnlyCollection<ServiceOrderWorkshop>? workshopServices = null;
        IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices = Array.Empty<ServiceOrderWorkshop>();
        if (request.WorkshopServiceIds is not null)
        {
            (workshopServices, newWorkshopServices) = await ResolveWorkshopServicesAsync(
                serviceOrder,
                request.WorkshopServiceIds,
                cancellationToken);
        }

        serviceOrder.Update(
            request.MechanicId,
            request.Description,
            request.CheckList,
            parts,
            workshopServices);

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus();

        await _serviceOrderRepository.UpdateAsync(serviceOrder, newParts, newWorkshopServices, cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);
        if (previousStatus != ServiceOrderStatus.AwaitingApproval &&
            serviceOrder.Status == ServiceOrderStatus.AwaitingApproval)
        {
            var budget = await _budgets.OpenFromServiceOrderAsync(serviceOrder.Id, cancellationToken);
            var customer = await _customerRepository.GetByIdAsync(serviceOrder.CustomerId, cancellationToken)
                ?? throw new InvalidOperationException("Customer was not found.");

            await _notifications.SendBudgetAwaitingApprovalAsync(
                customer.Name,
                customer.Email,
                budget,
                cancellationToken);
        }
        return MapDetail(serviceOrder);
    }

    private static bool HasNewItems(ServiceOrder serviceOrder, UpdateServiceOrderRequest request)
    {
        var hasNewParts = request.Parts?.Any(item =>
            serviceOrder.Parts.All(existing => existing.PartId != item.PartId)) ?? false;

        var hasNewWorkshopServices = request.WorkshopServiceIds?.Any(id =>
            serviceOrder.WorkshopServices.All(existing => existing.WorkshopServiceId != id)) ?? false;

        return hasNewParts || hasNewWorkshopServices;
    }

    public async Task<ServiceOrderDetailResponse> ApproveAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await GetAwaitingApprovalServiceOrderAsync(serviceOrderId, cancellationToken);

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus(clientApproved: true);

        await _serviceOrderRepository.UpdateAsync(
            serviceOrder,
            Array.Empty<ServiceOrderPart>(),
            Array.Empty<ServiceOrderWorkshop>(),
            cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);

        return MapDetail(serviceOrder);
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

        return MapDetail(serviceOrder);
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

        return MapDetail(serviceOrder);
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

        return MapDetail(serviceOrder);
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

            stock.AddQuantity(part.QuantityUsed);
            await _stocks.UpdateAsync(stock, cancellationToken);
        }
    }

    private async Task<(IReadOnlyCollection<ServiceOrderPart> All, IReadOnlyCollection<ServiceOrderPart> New)> ResolvePartsAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<AddPartToServiceOrderRequest> items,
        CancellationToken cancellationToken)
    {
        var parts = new List<ServiceOrderPart>();
        var newParts = new List<ServiceOrderPart>();
        var touchedStocks = new Dictionary<Guid, StockPart>();

        foreach (var item in items)
        {
            var part = await _parts.GetByIdAsync(item.PartId, cancellationToken);
            if (part is null)
            {
                throw new InvalidOperationException($"Part '{item.PartId}' was not found.");
            }

            var stock = await GetTouchedStockAsync(touchedStocks, part, cancellationToken);

            var serviceOrderPart = serviceOrder.Parts.FirstOrDefault(existing => existing.PartId == item.PartId);
            if (serviceOrderPart is null)
            {
                ConsumeStock(stock, part, item.Quantity);
                serviceOrderPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, item.Quantity);
                serviceOrderPart.OrderService = serviceOrder;
                newParts.Add(serviceOrderPart);
            }
            else
            {
                var delta = item.Quantity - serviceOrderPart.QuantityUsed;
                if (delta > 0)
                {
                    ConsumeStock(stock, part, delta);
                }
                else if (delta < 0)
                {
                    stock.AddQuantity(-delta);
                }

                serviceOrderPart.UpdateQuantity(item.Quantity);
            }

            serviceOrderPart.Part = part;
            parts.Add(serviceOrderPart);
        }

        foreach (var stock in touchedStocks.Values)
        {
            await _stocks.UpdateAsync(stock, cancellationToken);
        }

        return (parts, newParts);
    }

    private async Task<StockPart> GetTouchedStockAsync(
        Dictionary<Guid, StockPart> touchedStocks,
        Part part,
        CancellationToken cancellationToken)
    {
        if (touchedStocks.TryGetValue(part.Id, out var stock))
        {
            return stock;
        }

        stock = await _stocks.GetByPartIdAsync(part.Id, cancellationToken);
        if (stock is null)
        {
            throw new InvalidOperationException($"There is no stock registered for part '{part.Name}'.");
        }

        touchedStocks[part.Id] = stock;
        return stock;
    }

    private static void ConsumeStock(StockPart stock, Part part, int quantity)
    {
        if (stock.Quantity < quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for part '{part.Name}'. Available: {stock.Quantity}, requested: {quantity}.");
        }

        stock.RemoveQuantity(quantity);
    }

    private async Task<(IReadOnlyCollection<ServiceOrderWorkshop> All, IReadOnlyCollection<ServiceOrderWorkshop> New)> ResolveWorkshopServicesAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<Guid> workshopServiceIds,
        CancellationToken cancellationToken)
    {
        var workshopServices = new List<ServiceOrderWorkshop>();
        var newWorkshopServices = new List<ServiceOrderWorkshop>();

        foreach (var id in workshopServiceIds)
        {
            var workshopService = await _workshopServices.GetByIdAsync(id, cancellationToken);
            if (workshopService is null)
            {
                throw new InvalidOperationException($"Workshop service '{id}' was not found.");
            }

            var serviceOrderWorkshop = serviceOrder.WorkshopServices
                .FirstOrDefault(existing => existing.WorkshopServiceId == id);

            if (serviceOrderWorkshop is null)
            {
                serviceOrderWorkshop = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id);
                serviceOrderWorkshop.ServiceOrder = serviceOrder;
                newWorkshopServices.Add(serviceOrderWorkshop);
            }

            serviceOrderWorkshop.WorkshopService = workshopService;
            workshopServices.Add(serviceOrderWorkshop);
        }

        return (workshopServices, newWorkshopServices);
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

    private static ServiceOrderDetailResponse MapDetail(ServiceOrder order) =>
        new(
            order.Id,
            order.CustomerId,
            order.VehicleId,
            order.MechanicId,
            order.Description,
            order.CheckList,
            order.Status,
            order.CreatedAt,
            order.TotalParts,
            order.Parts.Select(part => new ServiceOrderPartResponse(part.Id, part.PartId, part.QuantityUsed)).ToList(),
            order.WorkshopServices.Select(service => new ServiceOrderWorkshopResponse(service.Id, service.WorkshopServiceId)).ToList());
}
