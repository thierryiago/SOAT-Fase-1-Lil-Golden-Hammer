namespace Oficina.Application.Stocks.UseCases;

public class AdjustStockUseCase(
    IStockRepository stocks,
    StockService stockService)
{
    private readonly IStockRepository _stocks = stocks;
    private readonly StockService _stockService = stockService;

    public async Task<StockResponse> AdjustAsync(
        Guid partId,
        StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        StockService.ValidateNonNegativeMovement(request.Quantity);
        await _stockService.EnsurePartExistsAsync(partId, cancellationToken);

        var stock = await _stockService.GetOrCreateStockAsync(partId, cancellationToken);
        stock.SetQuantity(request.Quantity);
        await _stocks.UpdateAsync(stock, cancellationToken);
        return StockResponseMapper.Map(stock);
    }
}
