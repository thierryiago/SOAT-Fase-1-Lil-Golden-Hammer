using Oficina.Application.Common;
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

    public async Task<PagedResponse<StockResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var stocks = await _stocks.ListAsync(cancellationToken);
        var query = stocks
            .OrderBy(stock => stock.PartId)
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<StockResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var stock = await _stocks.GetByIdAsync(id, cancellationToken);
        return stock is null ? null : Map(stock);
    }

    public async Task<StockResponse> CreateAsync(
        CreateStockRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePartExistsAsync(request.PartId, cancellationToken);

        var existing = await _stocks.GetByPartIdAsync(request.PartId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Stock already exists for the informed part.");
        }

        var stock = StockPart.Create(request.PartId, request.Quantity);
        await _stocks.AddAsync(stock, cancellationToken);
        return Map(stock);
    }

    public async Task<StockResponse> EntryAsync(
        Guid partId,
        StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePartExistsAsync(partId, cancellationToken);

        var stock = await GetOrCreateStockAsync(partId, cancellationToken);
        stock.AddQuantity(request.Quantity);
        await _stocks.UpdateAsync(stock, cancellationToken);
        return Map(stock);
    }

    public async Task<StockResponse> ConsumeAsync(
        Guid partId,
        StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePartExistsAsync(partId, cancellationToken);

        var stock = await GetOrCreateStockAsync(partId, cancellationToken);
        stock.RemoveQuantity(request.Quantity);
        await _stocks.UpdateAsync(stock, cancellationToken);
        return Map(stock);
    }

    public async Task<StockResponse> AdjustAsync(
        Guid partId,
        StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePartExistsAsync(partId, cancellationToken);

        var stock = await GetOrCreateStockAsync(partId, cancellationToken);
        stock.AdjustQuantity(request.Quantity);
        await _stocks.UpdateAsync(stock, cancellationToken);
        return Map(stock);
    }

    private async Task<StockPart> GetOrCreateStockAsync(Guid partId, CancellationToken cancellationToken)
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

    private async Task EnsurePartExistsAsync(Guid partId, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(partId, cancellationToken);
        if (part is null || !part.IsActive)
        {
            throw new KeyNotFoundException("Part was not found.");
        }
    }

    private static StockResponse Map(StockPart stockPart) =>
        new(stockPart.Id, stockPart.PartId, stockPart.Quantity, new DateTimeOffset(stockPart.CreatedDate));
}
