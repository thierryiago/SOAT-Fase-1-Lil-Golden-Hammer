using System.Collections.Concurrent;
using Oficina.Application.Stocks;
using Oficina.Domain.Stocks;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryStockRepository : IStockRepository
{
    private readonly ConcurrentDictionary<Guid, Stock> _stocksById = new();
    private readonly ConcurrentDictionary<Guid, Guid> _stockIdsByPartId = new();

    public Task<IReadOnlyCollection<Stock>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Stock>>(_stocksById.Values.OrderBy(stock => stock.PartId).ToList());

    public Task<Stock?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _stocksById.TryGetValue(id, out var stock);
        return Task.FromResult(stock);
    }

    public Task<Stock?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken)
    {
        if (_stockIdsByPartId.TryGetValue(partId, out var id) &&
            _stocksById.TryGetValue(id, out var stock))
        {
            return Task.FromResult<Stock?>(stock);
        }

        return Task.FromResult<Stock?>(null);
    }

    public Task AddAsync(Stock stock, CancellationToken cancellationToken)
    {
        _stocksById[stock.Id] = stock;
        _stockIdsByPartId[stock.PartId] = stock.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Stock stock, CancellationToken cancellationToken)
    {
        _stocksById[stock.Id] = stock;
        _stockIdsByPartId[stock.PartId] = stock.Id;
        return Task.CompletedTask;
    }
}
