using Microsoft.EntityFrameworkCore;
using Oficina.Application.Stocks;
using Oficina.Domain.Stock;

namespace Oficina.Infrastructure.Persistence;

public sealed class StockPartRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockPartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken) =>
        await _context.StockParts
            .AsNoTracking()
            .OrderBy(stockPart => stockPart.PartId)
            .ToListAsync(cancellationToken);

    public Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.StockParts.FirstOrDefaultAsync(stockPart => stockPart.Id == id, cancellationToken);

    public Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken) =>
        _context.StockParts.FirstOrDefaultAsync(stockPart => stockPart.PartId == partId, cancellationToken);

    public async Task AddAsync(StockPart stockPart, CancellationToken cancellationToken)
    {
        await _context.StockParts.AddAsync(stockPart, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken)
    {
        _context.StockParts.Update(stockPart);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
