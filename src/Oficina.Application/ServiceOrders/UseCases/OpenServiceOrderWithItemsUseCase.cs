using Oficina.Application.Budgets.UseCases;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Notifications.UseCases;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrderHistories;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrderHistories;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Stocks;
using Oficina.Domain.Vehicles;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Application.ServiceOrders.UseCases;

public sealed class OpenServiceOrderWithItemsUseCase(
    IServiceOrderRepository serviceOrders,
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    IPartRepository parts,
    IWorkshopServiceRepository workshopServices,
    IStockRepository stocks,
    IServiceOrderHistoryRepository history,
    OpenBudgetFromServiceOrderUseCase openBudget,
    SendServiceOrderOpenedEmailUseCase sendOpenedEmail)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly ICustomerRepository _customerRepository = customers;
    private readonly IVehicleRepository _vehicleRepository = vehicles;
    private readonly IPartRepository _parts = parts;
    private readonly IWorkshopServiceRepository _workshopServices = workshopServices;
    private readonly IStockRepository _stocks = stocks;
    private readonly IServiceOrderHistoryRepository _history = history;
    private readonly OpenBudgetFromServiceOrderUseCase _openBudget = openBudget;
    private readonly SendServiceOrderOpenedEmailUseCase _sendOpenedEmail = sendOpenedEmail;

    public async Task<ServiceOrderDetailResponse> ExecuteAsync(
        OpenServiceOrderWithItemsRequest request,
        CancellationToken cancellationToken)
    {
        var (customer, isNewCustomer) = await ResolveCustomerAsync(request.Customer, cancellationToken);
        var (vehicle, isNewVehicle) = await ResolveVehicleAsync(request.Vehicle, customer.Id, cancellationToken);
        var resolvedServices = await ResolveWorkshopServicesAsync(request.WorkshopServiceIds, cancellationToken);
        var resolvedParts = await ResolvePartsAsync(request.Parts ?? [], cancellationToken);
        var partStocks = await GetAvailableStocksAsync(resolvedParts, cancellationToken);

        if (isNewCustomer)
        {
            await _customerRepository.AddAsync(customer, cancellationToken);
        }

        if (isNewVehicle)
        {
            await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        }

        var serviceOrder = ServiceOrder.Open(customer.Id, vehicle.Id, request.Description);
        foreach (var workshopService in resolvedServices)
        {
            var serviceOrderWorkshop = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id);
            serviceOrderWorkshop.ServiceOrder = serviceOrder;
            serviceOrderWorkshop.WorkshopService = workshopService;
            serviceOrder.AddWorkshopService(serviceOrderWorkshop);
        }

        foreach (var (part, quantity) in resolvedParts)
        {
            var serviceOrderPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, quantity);
            serviceOrderPart.OrderService = serviceOrder;
            serviceOrderPart.Part = part;
            serviceOrder.AddPart(serviceOrderPart);
        }

        var approvalPath = serviceOrder.RequestApprovalOnOpening();

        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);

        foreach (var (part, quantity) in resolvedParts)
        {
            var stock = partStocks[part.Id];
            stock.Reserve(quantity);
            await _stocks.UpdateAsync(stock, cancellationToken);
        }

        await RecordHistoryAsync(serviceOrder.Id, [ServiceOrderStatus.Created, .. approvalPath], cancellationToken);

        var budget = await _openBudget.OpenFromServiceOrderAsync(serviceOrder.Id, cancellationToken);
        await _sendOpenedEmail.ExecuteAsync(serviceOrder, customer, vehicle, budget, cancellationToken);

        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }

    private async Task<(Customer Customer, bool IsNew)> ResolveCustomerAsync(
        OpenServiceOrderWithItemsCustomerRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Document))
        {
            throw new InvalidOperationException("The customer document is required.");
        }

        var document = DocumentValidatorAttribute.NormalizeDocument(request.Document);
        var existing = await _customerRepository.GetByDocumentAsync(document, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                throw new InvalidOperationException("The customer with the informed document has been disabled.");
            }

            return (existing, false);
        }

        var customer = Customer.Create(
            request.Name ?? string.Empty,
            request.Email ?? string.Empty,
            request.TelephoneNumber ?? string.Empty,
            document);
        return (customer, true);
    }

    private async Task<(Vehicle Vehicle, bool IsNew)> ResolveVehicleAsync(
        OpenServiceOrderWithItemsVehicleRequest? request,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Plate))
        {
            throw new InvalidOperationException("The vehicle plate is required.");
        }

        var existing = await _vehicleRepository.GetByPlateAsync(Vehicle.NormalizePlate(request.Plate), cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                throw new ConflictException("The vehicle with the informed plate has been disabled.");
            }

            if (existing.CustomerId != customerId)
            {
                throw new ConflictException("The vehicle with the informed plate belongs to another customer.");
            }

            return (existing, false);
        }

        var vehicle = Vehicle.Create(
            customerId,
            request.Plate,
            request.Brand ?? string.Empty,
            request.Model ?? string.Empty,
            request.Year ?? 0,
            request.Category);
        return (vehicle, true);
    }

    private async Task<IReadOnlyCollection<WorkshopService>> ResolveWorkshopServicesAsync(
        IReadOnlyCollection<Guid>? workshopServiceIds,
        CancellationToken cancellationToken)
    {
        if (workshopServiceIds is null || workshopServiceIds.Count == 0)
        {
            throw new InvalidOperationException("At least one workshop service is required.");
        }

        ServiceOrder.EnsureNoRepeatedWorkshopServices(workshopServiceIds);

        var resolved = new List<WorkshopService>();
        foreach (var id in workshopServiceIds)
        {
            var workshopService = await _workshopServices.GetByIdAsync(id, cancellationToken);
            if (workshopService is null)
            {
                throw new InvalidOperationException($"Workshop service '{id}' was not found.");
            }

            resolved.Add(workshopService);
        }

        return resolved;
    }

    private async Task<IReadOnlyCollection<(Part Part, int Quantity)>> ResolvePartsAsync(
        IReadOnlyCollection<OpenServiceOrderWithItemsPartRequest> items,
        CancellationToken cancellationToken)
    {
        ServiceOrder.EnsureNoRepeatedParts(items.Select(item => item.PartId));

        var resolved = new List<(Part Part, int Quantity)>();
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

            resolved.Add((part, item.Quantity));
        }

        return resolved;
    }

    private async Task<IReadOnlyDictionary<Guid, StockPart>> GetAvailableStocksAsync(
        IReadOnlyCollection<(Part Part, int Quantity)> items,
        CancellationToken cancellationToken)
    {
        var partStocks = new Dictionary<Guid, StockPart>();
        foreach (var (part, quantity) in items)
        {
            var stock = await _stocks.GetByPartIdAsync(part.Id, cancellationToken);
            if (stock is null)
            {
                throw new InvalidOperationException($"There is no stock registered for part '{part.Name}'.");
            }

            stock.EnsureCanReserve(quantity);
            partStocks[part.Id] = stock;
        }

        return partStocks;
    }

    private async Task RecordHistoryAsync(
        Guid serviceOrderId,
        IEnumerable<ServiceOrderStatus> statuses,
        CancellationToken cancellationToken)
    {
        foreach (var status in statuses)
        {
            var entry = ServiceOrderHistory.Create(serviceOrderId, status.ToString());
            await _history.AddAsync(entry, cancellationToken);
        }
    }
}
