using Oficina.Domain.Stock;

namespace Oficina.Application.Stocks;

public interface IStockRepository
{
    Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken);
    Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken);
    Task AddAsync(StockPart stockPart, CancellationToken cancellationToken);
    Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken);
}
