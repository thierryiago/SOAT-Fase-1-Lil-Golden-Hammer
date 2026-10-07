using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Application.OrderServiceHistory.UseCases;

public sealed class CreateServiceOrderHistoryUseCase(IServiceOrderHistoryRepository historyRepository)
{
    private readonly IServiceOrderHistoryRepository _historyRepository = historyRepository;

    public async Task<ServiceOrderHistory> CreateAsync(
        Guid serviceOrderId,
        string? statusName,
        CancellationToken cancellationToken)
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
