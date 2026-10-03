using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Notifications;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Parts;
using Oficina.Application.Stocks;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Stock;

namespace Oficina.Application.ServiceOrders.UseCases;

public sealed class UpdateServiceOrderUseCase(
    IServiceOrderRepository serviceOrders,
    IPartRepository parts,
    IWorkshopServiceRepository workshopServices,
    IStockRepository stocks,
    IServiceOrderHistoryRepository history,
    IBudgetService budgets,
    ICustomerRepository customers,
    INotificationEmailSender notificationEmailSender)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly IPartRepository _parts = parts;
    private readonly IWorkshopServiceRepository _workshopServices = workshopServices;
    private readonly IStockRepository _stocks = stocks;
    private readonly IServiceOrderHistoryRepository _history = history;
    private readonly IBudgetService _budgets = budgets;
    private readonly ICustomerRepository _customerRepository = customers;
    private readonly INotificationEmailSender _notificationEmailSender = notificationEmailSender;

    public async Task<ServiceOrderDetailResponse> ExecuteAsync(
        UpdateServiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(request.ServiceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }
        var hasItemChanges = HasItemChanges(serviceOrder, request);
        serviceOrder.ValidateUpdate(request.MechanicId, hasItemChanges);

        if (serviceOrder.Status == ServiceOrderStatus.InExecution &&
            hasItemChanges &&
            request.WorkshopServiceIds is { Count: 0 })
        {
            throw new InvalidOperationException(
                "The service order must have at least one workshop service for reapproval.");
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

        IReadOnlyCollection<ServiceOrderPart>? parts = null;
        IReadOnlyCollection<ServiceOrderPart> newParts = Array.Empty<ServiceOrderPart>();
        if (request.Parts is not null)
        {
            (parts, newParts) = await ResolvePartsAsync(serviceOrder, request.Parts, cancellationToken);
        }

        var previousStatus = serviceOrder.Status;
        serviceOrder.Update(
            request.MechanicId,
            request.Description,
            request.CheckList,
            parts,
            workshopServices);

        if (previousStatus == ServiceOrderStatus.InExecution && hasItemChanges)
        {
            serviceOrder.RequestReapproval(hasItemChanges);
        }
        else
        {
            serviceOrder.UpdateStatus();
        }

        await _serviceOrderRepository.UpdateAsync(serviceOrder, newParts, newWorkshopServices, cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);
        if (previousStatus != ServiceOrderStatus.AwaitingApproval &&
            serviceOrder.Status == ServiceOrderStatus.AwaitingApproval)
        {
            var budget = await _budgets.OpenFromServiceOrderAsync(serviceOrder.Id, cancellationToken);
            var customer = await _customerRepository.GetByIdAsync(serviceOrder.CustomerId, cancellationToken)
                ?? throw new InvalidOperationException("Customer was not found.");

            await _notificationEmailSender.SendBudgetAwaitingApprovalAsync(
                customer.Name,
                customer.Email,
                budget,
                cancellationToken);
        }
        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }

    private static bool HasItemChanges(ServiceOrder serviceOrder, UpdateServiceOrderRequest request)
    {
        var partsChanged = request.Parts is not null &&
            (request.Parts.Count != serviceOrder.Parts.Count ||
             request.Parts.Any(item =>
                 serviceOrder.Parts.All(existing =>
                     existing.PartId != item.PartId || existing.QuantityUsed != item.Quantity)));

        var workshopServicesChanged = request.WorkshopServiceIds is not null &&
            (request.WorkshopServiceIds.Count != serviceOrder.WorkshopServices.Count ||
             request.WorkshopServiceIds.Any(id =>
                 serviceOrder.WorkshopServices.All(existing => existing.WorkshopServiceId != id)));

        return partsChanged || workshopServicesChanged;
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

    private async Task<(IReadOnlyCollection<ServiceOrderPart> All, IReadOnlyCollection<ServiceOrderPart> New)> ResolvePartsAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<AddPartToServiceOrderRequest> items,
        CancellationToken cancellationToken)
    {
        var parts = new List<ServiceOrderPart>();
        var newParts = new List<ServiceOrderPart>();
        var touchedStocks = new Dictionary<Guid, StockPart>();
        var stockDeltas = new Dictionary<Guid, int>();
        var quantitiesToUpdate = new List<(ServiceOrderPart Part, int Quantity)>();

        ServiceOrder.EnsureNoRepeatedParts(items.Select(item => item.PartId));

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(item.Quantity),
                    "Quantity must be greater than zero.");
            }

            var part = await _parts.GetByIdAsync(item.PartId, cancellationToken);
            if (part is null)
            {
                throw new InvalidOperationException($"Part '{item.PartId}' was not found.");
            }

            var serviceOrderPart = serviceOrder.Parts.FirstOrDefault(existing => existing.PartId == item.PartId);
            var currentQuantity = serviceOrderPart?.QuantityUsed ?? 0;
            var delta = item.Quantity - currentQuantity;
            if (delta != 0)
            {
                var stock = await GetTouchedStockAsync(touchedStocks, part, cancellationToken);
                stockDeltas[part.Id] = delta;
                if (delta > 0)
                {
                    stock.EnsureCanReserve(delta);
                }
            }

            if (serviceOrderPart is null)
            {
                serviceOrderPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, item.Quantity);
                serviceOrderPart.OrderService = serviceOrder;
                newParts.Add(serviceOrderPart);
            }
            else
            {
                quantitiesToUpdate.Add((serviceOrderPart, item.Quantity));
            }

            serviceOrderPart.Part = part;
            parts.Add(serviceOrderPart);
        }

        var requestedPartIds = items.Select(item => item.PartId).ToHashSet();
        foreach (var removedPart in serviceOrder.Parts.Where(part => !requestedPartIds.Contains(part.PartId)))
        {
            var stock = await _stocks.GetByPartIdAsync(removedPart.PartId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"There is no stock registered for part '{removedPart.PartId}'.");
            touchedStocks[removedPart.PartId] = stock;
            stockDeltas[removedPart.PartId] = -removedPart.QuantityUsed;
        }

        foreach (var (part, quantity) in quantitiesToUpdate)
        {
            part.UpdateQuantity(quantity);
        }

        foreach (var (partId, delta) in stockDeltas)
        {
            var stock = touchedStocks[partId];
            if (delta > 0)
            {
                stock.Reserve(delta);
            }
            else
            {
                stock.Release(-delta);
            }
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

    private async Task<(IReadOnlyCollection<ServiceOrderWorkshop> All, IReadOnlyCollection<ServiceOrderWorkshop> New)> ResolveWorkshopServicesAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<Guid> workshopServiceIds,
        CancellationToken cancellationToken)
    {
        var workshopServices = new List<ServiceOrderWorkshop>();
        var newWorkshopServices = new List<ServiceOrderWorkshop>();

        ServiceOrder.EnsureNoRepeatedWorkshopServices(workshopServiceIds);

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
}
