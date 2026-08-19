using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Application.OrderServiceHistory;

public sealed class ServiceOrderHistoryService
{
    private readonly IServiceOrderHistoryRepository _historyRepository;

    public ServiceOrderHistoryService(IServiceOrderHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task<IReadOnlyCollection<ServiceOrderHistoryResponse>> FindAllAsync(CancellationToken cancellationToken)
    {
        var history = await _historyRepository.ListAsync(cancellationToken);
        return history.Select(Map).ToList();
    }

    public async Task<IReadOnlyCollection<ServiceOrderHistoryResponse>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        if (serviceOrderId == Guid.Empty)
        {
            throw new ArgumentException("Service order id is required.", nameof(serviceOrderId));
        }

        var history = await _historyRepository.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
        return history.Select(Map).ToList();
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

    private static ServiceOrderHistoryResponse Map(ServiceOrderHistory history) =>
        new(history.Id, history.OrderServiceId, history.StatusName, history.CreatedDate);
}
