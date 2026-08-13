using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public sealed class ServiceOrderService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _parts;

    public ServiceOrderService(
        IServiceOrderRepository serviceOrders,
        ICustomerRepository customers,
        IPartRepository parts)
    {
        _serviceOrderRepository = serviceOrders;
        _customerRepository = customers;
        _parts = parts;
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
        Task<ServiceOrder?> os = GetByIdAsync(request.ServiceOrderId, cancellationToken);
        if (os is null || os.Result is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }
        ServiceOrder serviceOrder = os.Result;

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer was not found!");
        }
        else if (customer.Id != serviceOrder.CustomerId)
        {
            throw new InvalidOperationException("The customer cannot be changed on the Service Order!");
        }

        //request.UpdateStatus();

        await _serviceOrderRepository.UpdateAsync(serviceOrder, cancellationToken);
        return serviceOrder;
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
