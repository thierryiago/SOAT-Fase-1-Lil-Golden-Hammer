using Oficina.Application.Common;

namespace Oficina.Application.Stocks.UseCases.Queries;

public class ListStocksUseCase(
    IStockRepository stocks)
{
    private readonly IStockRepository _stocks = stocks;

    public async Task<PagedResponse<StockResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var stocks = await _stocks.ListAsync(cancellationToken);
        var query = stocks
            .OrderBy(stock => stock.PartId)
            .Select(StockResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}
