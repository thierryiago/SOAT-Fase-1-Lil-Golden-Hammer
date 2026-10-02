using Oficina.Application.Parts;
using Oficina.Domain.Stock;

namespace Oficina.Application.Stocks;

public sealed class StockService
{
    private readonly IStockRepository _stocks;
    private readonly IPartRepository _parts;

    public StockService(IStockRepository stocks, IPartRepository parts)
    {
        _stocks = stocks;
        _parts = parts;
    }

    public async Task<StockPart> GetOrCreateStockAsync(Guid partId, CancellationToken cancellationToken)
    {
        var existing = await _stocks.GetByPartIdAsync(partId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var stock = StockPart.Create(partId, 0);
        await _stocks.AddAsync(stock, cancellationToken);
        return stock;
    }

    public async Task EnsurePartExistsAsync(Guid partId, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(partId, cancellationToken);
        if (part is null || !part.IsActive)
        {
            throw new KeyNotFoundException("Part was not found.");
        }
    }

    public static void ValidateNonNegativeMovement(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock movement quantity cannot be negative.");
        }
    }
}
