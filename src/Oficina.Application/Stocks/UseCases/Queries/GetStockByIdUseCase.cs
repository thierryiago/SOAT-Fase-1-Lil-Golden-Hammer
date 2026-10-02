namespace Oficina.Application.Stocks.UseCases.Queries;

public class GetStockByIdUseCase(
    IStockRepository stocks)
{
    private readonly IStockRepository _stocks = stocks;

    public async Task<StockResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var stock = await _stocks.GetByIdAsync(id, cancellationToken);
        return stock is null ? null : StockResponseMapper.Map(stock);
    }
}
