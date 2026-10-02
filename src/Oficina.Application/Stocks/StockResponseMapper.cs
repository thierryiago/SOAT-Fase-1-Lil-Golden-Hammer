using Oficina.Domain.Stock;

namespace Oficina.Application.Stocks;

internal static class StockResponseMapper
{
    public static StockResponse Map(StockPart stockPart) =>
        new(
            stockPart.Id,
            stockPart.PartId,
            stockPart.Quantity,
            new DateTimeOffset(stockPart.CreatedDate));
}
