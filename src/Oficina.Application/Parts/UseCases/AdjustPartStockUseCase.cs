using Oficina.Application.Stocks;
using Oficina.Domain.Stock;

namespace Oficina.Application.Parts.UseCases;

public class AdjustPartStockUseCase(
    IStockRepository stocks,
    PartService partService)
{
    private readonly IStockRepository _stocks = stocks;
    private readonly PartService _partService = partService;

    public async Task<PartResponse> AdjustStockAsync(
        Guid id,
        AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Stock adjustment reason is required.", nameof(request.Reason));
        }

        await _partService.GetActivePartAsync(id, cancellationToken);
        var stockPart = await _stocks.GetByPartIdAsync(id, cancellationToken);

        if (stockPart is null)
        {
            stockPart = StockPart.Create(id, 0);
            stockPart.AdjustQuantity(request.Quantity);
            await _stocks.AddAsync(stockPart, cancellationToken);
        }
        else
        {
            stockPart.AdjustQuantity(request.Quantity);
            await _stocks.UpdateAsync(stockPart, cancellationToken);
        }

        var part = await _partService.GetActivePartAsync(id, cancellationToken);
        return PartResponseMapper.Map(part);
    }
}
