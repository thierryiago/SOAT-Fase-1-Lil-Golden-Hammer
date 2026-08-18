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

    public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken) =>
        _serviceOrderRepository.ListAsync(cancellationToken);

    public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _serviceOrderRepository.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceOrder> OpenAsync(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        var serviceOrder = ServiceOrder.Open(request.CustomerId, request.Description);
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        return serviceOrder;
    }

    public async Task<ServiceOrder> UpdateAsync(UpdateServiceOrderRequest request, CancellationToken cancellationToken)
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

        serviceOrder.Update(
            request.MechanicId,
            request.Description,
            request.CheckList,
            parts,
            workshopServices);

        await _serviceOrderRepository.UpdateAsync(serviceOrder, newParts, newWorkshopServices, cancellationToken);
        return serviceOrder;
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

}
