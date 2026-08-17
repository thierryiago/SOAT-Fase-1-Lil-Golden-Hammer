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

        var parts = request.Parts is null
            ? null
            : await ResolvePartsAsync(serviceOrder, request.Parts, cancellationToken);

        var workshopServices = request.WorkshopServiceIds is null
            ? null
            : await ResolveWorkshopServicesAsync(serviceOrder, request.WorkshopServiceIds, cancellationToken);

        serviceOrder.Update(
            request.MechanicId,
            request.Description,
            request.CheckList,
            parts,
            workshopServices);

        await _serviceOrderRepository.UpdateAsync(serviceOrder, cancellationToken);
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

    private async Task<IReadOnlyCollection<ServiceOrderPart>> ResolvePartsAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<AddPartToServiceOrderRequest> items,
        CancellationToken cancellationToken)
    {
        var parts = new List<ServiceOrderPart>();

        foreach (var item in items)
        {
            var part = await _parts.GetByIdAsync(item.PartId, cancellationToken);
            if (part is null)
            {
                throw new InvalidOperationException($"Part '{item.PartId}' was not found.");
            }

            var serviceOrderPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, item.Quantity);
            serviceOrderPart.Part = part;
            serviceOrderPart.OrderService = serviceOrder;
            parts.Add(serviceOrderPart);
        }

        return parts;
    }

    private async Task<IReadOnlyCollection<ServiceOrderWorkshop>> ResolveWorkshopServicesAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<Guid> workshopServiceIds,
        CancellationToken cancellationToken)
    {
        var workshopServices = new List<ServiceOrderWorkshop>();

        foreach (var id in workshopServiceIds)
        {
            var workshopService = await _workshopServices.GetByIdAsync(id, cancellationToken);
            if (workshopService is null)
            {
                throw new InvalidOperationException($"Workshop service '{id}' was not found.");
            }

            var serviceOrderWorkshop = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id);
            serviceOrderWorkshop.WorkshopService = workshopService;
            serviceOrderWorkshop.ServiceOrder = serviceOrder;
            workshopServices.Add(serviceOrderWorkshop);
        }

        return workshopServices;
    }

    public async Task<ServiceOrder> AddPartAsync(
        Guid serviceOrderId,
        AddPartToServiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        /*
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service order was not found.");
        }

        var part = await _parts.GetByIdAsync(request.PartId, cancellationToken);
        if (part is null)
        {
            throw new InvalidOperationException("Part was not found.");
        }

        // part.WithdrawStock(request.Quantity);
        serviceOrder.AddPart(part.Id, part.Name, request.Quantity, part.UnitPrice);
        await _parts.UpdateAsync(part, cancellationToken);
        await _serviceOrderRepository.UpdateAsync(serviceOrder, cancellationToken);
        */
        return null;
    }
}
