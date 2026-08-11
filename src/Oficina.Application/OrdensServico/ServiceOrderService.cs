using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public sealed class ServiceOrderService
{
    private readonly IServiceOrderRepository _serviceOrders;
    private readonly ICustomerRepository _customers;
    private readonly IPartRepository _parts;

    public ServiceOrderService(
        IServiceOrderRepository serviceOrders,
        ICustomerRepository customers,
        IPartRepository parts)
    {
        _serviceOrders = serviceOrders;
        _customers = customers;
        _parts = parts;
    }

    public Task<IReadOnlyCollection<ServiceOrder>> ListAsync(CancellationToken cancellationToken) =>
        _serviceOrders.ListAsync(cancellationToken);

    public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _serviceOrders.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceOrder> OpenAsync(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        var serviceOrder = ServiceOrder.Open(request.CustomerId, request.Description);
        await _serviceOrders.AddAsync(serviceOrder, cancellationToken);
        return serviceOrder;
    }

    public async Task<ServiceOrder> AddPartAsync(
        Guid serviceOrderId,
        AddPartToServiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.GetByIdAsync(serviceOrderId, cancellationToken);
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
        await _serviceOrders.UpdateAsync(serviceOrder, cancellationToken);
        return serviceOrder;
    }
}
