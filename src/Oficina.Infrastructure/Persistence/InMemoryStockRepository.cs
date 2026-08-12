using System.Collections.Concurrent;
using Oficina.Application.Stocks;
using Oficina.Domain.Stock;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryStockRepository : IStockRepository
{
    private readonly ConcurrentDictionary<Guid, StockPart> _stocksById = new();
    private readonly ConcurrentDictionary<Guid, Guid> _stockIdsByPartId = new();

    public Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<StockPart>>(_stocksById.Values.OrderBy(stockPart => stockPart.PartId).ToList());

    public Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _stocksById.TryGetValue(id, out var stock);
        return Task.FromResult(stock);
    }

    public Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken)
    {
        if (_stockIdsByPartId.TryGetValue(partId, out var id) &&
            _stocksById.TryGetValue(id, out var stock))
        {
            return Task.FromResult<StockPart?>(stock);
        }

        return Task.FromResult<StockPart?>(null);
    }

    public Task AddAsync(StockPart stockPart, CancellationToken cancellationToken)
    {
        _stocksById[stockPart.Id] = stockPart;
        _stockIdsByPartId[stockPart.PartId] = stockPart.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken)
    {
        _stocksById[stockPart.Id] = stockPart;
        _stockIdsByPartId[stockPart.PartId] = stockPart.Id;
        return Task.CompletedTask;
    }
}
