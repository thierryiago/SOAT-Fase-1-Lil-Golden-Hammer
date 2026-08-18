using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.Services;
using Oficina.Domain.OrderService;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public sealed class ServiceOrderService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _parts;
    private readonly IWorkshopServiceRepository _workshopServices;

    public ServiceOrderService(
        IServiceOrderRepository serviceOrders,
        ICustomerRepository customers,
        IPartRepository parts,
        IWorkshopServiceRepository workshopServices)
    {
        _serviceOrderRepository = serviceOrders;
        _customerRepository = customers;
        _parts = parts;
        _workshopServices = workshopServices;
    }

    public async Task<IReadOnlyCollection<ServiceOrderListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var serviceOrders = await _serviceOrders.ListAsync(cancellationToken);
        return serviceOrders.Select(MapListItem).ToList();
    }

    public async Task<ServiceOrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.GetByIdAsync(id, cancellationToken);
        return serviceOrder is null ? null : MapDetail(serviceOrder);
    }

    public async Task<ServiceOrderDetailResponse> OpenAsync(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        var serviceOrder = ServiceOrder.Open(request.CustomerId, request.Description);
        await _serviceOrders.AddAsync(serviceOrder, cancellationToken);
        return MapDetail(serviceOrder);
    }

    public async Task<ServiceOrderDetailResponse> AddPartAsync(
        Guid serviceOrderId,
        AddPartToServiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await GetByIdAsync(request.ServiceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }

        await CheckCustomerAsync(serviceOrder, request.CustomerId, cancellationToken);

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

        // part.WithdrawStock(request.Quantity);
        serviceOrder.AddPart(part.Id, part.Name, request.Quantity, part.UnitPrice);
        await _parts.UpdateAsync(part, cancellationToken);
        await _serviceOrders.UpdateAsync(serviceOrder, cancellationToken);
        return MapDetail(serviceOrder);
    }

    private static ServiceOrderListItemResponse MapListItem(ServiceOrder serviceOrder) =>
        new(
            serviceOrder.Id,
            serviceOrder.CustomerId,
            serviceOrder.VehicleId,
            serviceOrder.MechanicId,
            serviceOrder.Description,
            serviceOrder.Status,
            serviceOrder.CreatedAt,
            serviceOrder.TotalParts);

    private static ServiceOrderDetailResponse MapDetail(ServiceOrder serviceOrder) =>
        new(
            serviceOrder.Id,
            serviceOrder.CustomerId,
            serviceOrder.VehicleId,
            serviceOrder.MechanicId,
            serviceOrder.Description,
            serviceOrder.CheckList,
            serviceOrder.Status,
            serviceOrder.CreatedAt,
            serviceOrder.TotalParts,
            serviceOrder.Parts
                .Select(part => new ServiceOrderPartResponse(part.Id, part.PartId, part.QuantityUsed))
                .ToList());
}
