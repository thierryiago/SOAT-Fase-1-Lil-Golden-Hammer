namespace Oficina.Application.OrderServiceHistory.UseCases.Queries;

public sealed class ListServiceOrderHistoryUseCase(IServiceOrderHistoryRepository historyRepository)
{
    private readonly IServiceOrderHistoryRepository _historyRepository = historyRepository;

    public async Task<IReadOnlyCollection<ServiceOrderHistoryResponse>> FindAllAsync(
        CancellationToken cancellationToken)
    {
        var history = await _historyRepository.ListAsync(cancellationToken);
        return history.Select(ServiceOrderHistoryResponseMapper.Map).ToList();
    }
}
