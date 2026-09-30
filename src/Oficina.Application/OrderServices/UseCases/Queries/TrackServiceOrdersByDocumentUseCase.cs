using Oficina.Application.Customers;
using Oficina.Domain.Customers;

namespace Oficina.Application.ServiceOrders.UseCases.Queries;

public sealed class TrackServiceOrdersByDocumentUseCase(
    IServiceOrderRepository serviceOrders,
    ICustomerRepository customers)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly ICustomerRepository _customerRepository = customers;

    public async Task<IReadOnlyCollection<ServiceOrderTrackingSummaryResponse>> ExecuteAsync(
        string document,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByDocumentAsync(
            Customer.NormalizeDocument(document),
            cancellationToken);
        if (customer is null)
        {
            return [];
        }

        var orders = await _serviceOrderRepository.ListByCustomerAsync(customer.Id, cancellationToken);
        return [.. orders
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new ServiceOrderTrackingSummaryResponse(
                order.Id,
                order.Status?.ToString(),
                order.Description,
                order.CreatedAt))];
    }
}
