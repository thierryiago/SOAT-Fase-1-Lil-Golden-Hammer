using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public interface IServiceOrderRepository
{
    Task<IReadOnlyCollection<ServiceOrder>> ListAsync(CancellationToken cancellationToken);
    Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken);
    Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken);
}
