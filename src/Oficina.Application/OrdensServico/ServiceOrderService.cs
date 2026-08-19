using Oficina.Application.Customers;
using Oficina.Application.OrdensServico;
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
        var orders = await _serviceOrderRepository.ListAsync(cancellationToken);
        return orders.Select(MapListItem).ToList();
    }

    public async Task<ServiceOrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : MapDetail(order);
    }

    public async Task<ServiceOrderDetailResponse> OpenAsync(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        var serviceOrder = ServiceOrder.Open(request.CustomerId, request.Description);
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

        serviceOrder.Update(
            request.MechanicId,
            request.Description,
            request.CheckList,
            parts,
            workshopServices);

        await _serviceOrderRepository.UpdateAsync(serviceOrder, newParts, newWorkshopServices, cancellationToken);
        return MapDetail(serviceOrder);
    }

    private async Task CheckCustomerAsync(
        ServiceOrder serviceOrder,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer was not found!");
        }

        if (customer.Id != serviceOrder.CustomerId)
        {
            throw new InvalidOperationException("The customer cannot be changed on the Service Order!");
        }
    }

    private async Task<(IReadOnlyCollection<ServiceOrderPart> All, IReadOnlyCollection<ServiceOrderPart> New)> ResolvePartsAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<AddPartToServiceOrderRequest> items,
        CancellationToken cancellationToken)
    {
        var parts = new List<ServiceOrderPart>();
        var newParts = new List<ServiceOrderPart>();

        foreach (var item in items)
        {
            var part = await _parts.GetByIdAsync(item.PartId, cancellationToken);
            if (part is null)
            {
                throw new InvalidOperationException($"Part '{item.PartId}' was not found.");
            }

            var serviceOrderPart = serviceOrder.Parts.FirstOrDefault(existing => existing.PartId == item.PartId);
            if (serviceOrderPart is null)
            {
                serviceOrderPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, item.Quantity);
                serviceOrderPart.OrderService = serviceOrder;
                newParts.Add(serviceOrderPart);
            }
            else
            {
                serviceOrderPart.UpdateQuantity(item.Quantity);
            }

            serviceOrderPart.Part = part;
            parts.Add(serviceOrderPart);
        }

        return (parts, newParts);
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
