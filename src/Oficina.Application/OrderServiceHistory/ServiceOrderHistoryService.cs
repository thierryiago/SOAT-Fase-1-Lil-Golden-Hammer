using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Application.OrderServiceHistory;

public sealed class ServiceOrderHistoryService
{
    private readonly IServiceOrderHistoryRepository _historyRepository;

    public ServiceOrderHistoryService(IServiceOrderHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task<List<ServiceOrderHistory>> FindAllAsync(CancellationToken cancellationToken)
    {
        return await _historyRepository.ListAsync(cancellationToken);
    }

    public async Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        if (serviceOrderId == Guid.Empty)
        {
            throw new ArgumentException("Service order id is required.", nameof(serviceOrderId));
        }

        return await _historyRepository.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
    }

    public async Task<ServiceOrderHistory> CreateAsync(Guid serviceOrderId, string? statusName, CancellationToken cancellationToken)
    {
        if (serviceOrderId == Guid.Empty)
        {
            throw new ArgumentException("Service order id is required.", nameof(serviceOrderId));
        }

        var history = ServiceOrderHistory.Create(serviceOrderId, statusName);
        await _historyRepository.AddAsync(history, cancellationToken);
        return history;
    }
}
