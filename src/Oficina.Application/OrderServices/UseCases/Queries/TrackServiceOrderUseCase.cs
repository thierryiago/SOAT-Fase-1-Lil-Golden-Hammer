using Oficina.Application.Customers;
using Oficina.Application.OrderServiceHistory;
using Oficina.Domain.Customers;

namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class TrackServiceOrderUseCase(
    IServiceOrderRepository serviceOrders,
    ICustomerRepository customers,
    IServiceOrderHistoryRepository history)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly ICustomerRepository _customerRepository = customers;
    private readonly IServiceOrderHistoryRepository _historyRepository = history;

    public async Task<ServiceOrderTrackingResponse?> ExecuteAsync(
        Guid serviceOrderId,
        string document,
        CancellationToken cancellationToken)
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

        var history = await _historyRepository.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
        var timeline = history
            .OrderByDescending(entry => entry.CreatedDate)
            .Select(entry => new ServiceOrderTrackingHistoryItem(entry.StatusName, entry.CreatedDate))
            .ToList();

        return new ServiceOrderTrackingResponse(
            order.Id,
            order.Status?.ToString(),
            order.Description,
            order.CreatedAt,
            timeline);
    }
}
