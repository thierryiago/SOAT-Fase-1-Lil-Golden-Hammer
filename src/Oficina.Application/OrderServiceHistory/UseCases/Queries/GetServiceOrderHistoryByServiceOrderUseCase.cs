namespace Oficina.Application.OrderServiceHistory.UseCases.Queries;

public sealed class GetServiceOrderHistoryByServiceOrderUseCase(IServiceOrderHistoryRepository historyRepository)
{
    private readonly IServiceOrderHistoryRepository _historyRepository = historyRepository;

    public async Task<IReadOnlyCollection<ServiceOrderHistoryResponse>> FindByServiceOrderAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken)
    {
        if (serviceOrderId == Guid.Empty)
        {
            throw new ArgumentException("Service order id is required.", nameof(serviceOrderId));
        }

        var history = await _historyRepository.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
        return history.Select(ServiceOrderHistoryResponseMapper.Map).ToList();
    }
}
