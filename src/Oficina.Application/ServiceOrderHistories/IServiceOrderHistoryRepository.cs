using Oficina.Domain.ServiceOrderHistories;

namespace Oficina.Application.ServiceOrderHistories;

public interface IServiceOrderHistoryRepository
{
    Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken);
    Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken);
    Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken);
}
