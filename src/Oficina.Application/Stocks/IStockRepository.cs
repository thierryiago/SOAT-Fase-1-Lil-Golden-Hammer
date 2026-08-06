using Oficina.Domain.Stocks;

namespace Oficina.Application.Stocks;

public interface IStockRepository
{
    Task<IReadOnlyCollection<Stock>> ListAsync(CancellationToken cancellationToken);
    Task<Stock?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Stock?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken);
    Task AddAsync(Stock stock, CancellationToken cancellationToken);
    Task UpdateAsync(Stock stock, CancellationToken cancellationToken);
}
