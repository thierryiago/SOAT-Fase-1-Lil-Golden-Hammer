using System.Collections.Concurrent;
using Oficina.Application.ServiceOrders;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryServiceOrderRepository : IServiceOrderRepository
{
    private readonly ConcurrentDictionary<Guid, ServiceOrder> _serviceOrders = new();

    public Task<IReadOnlyCollection<ServiceOrder>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<ServiceOrder>>(
            _serviceOrders.Values.OrderByDescending(serviceOrder => serviceOrder.CreatedAt).ToList());

    public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _serviceOrders.TryGetValue(id, out var serviceOrder);
        return Task.FromResult(serviceOrder);
    }

    public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
    {
        _serviceOrders[serviceOrder.Id] = serviceOrder;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
    {
        _serviceOrders[serviceOrder.Id] = serviceOrder;
        return Task.CompletedTask;
    }
}
