using Oficina.Application.Common;
using Oficina.Domain.Stock;

namespace Oficina.Application.Stocks.UseCases;

public class CreateStockUseCase(
    IStockRepository stocks,
    StockService stockService)
{
    private readonly IStockRepository _stocks = stocks;
    private readonly StockService _stockService = stockService;

    public async Task<StockResponse> CreateAsync(
        CreateStockRequest request,
        CancellationToken cancellationToken)
    {
        await _stockService.EnsurePartExistsAsync(request.PartId, cancellationToken);

        var existing = await _stocks.GetByPartIdAsync(request.PartId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Stock already exists for the informed part.");
        }

        var stock = StockPart.Create(request.PartId, request.Quantity);
        await _stocks.AddAsync(stock, cancellationToken);
        return StockResponseMapper.Map(stock);
    }
}
