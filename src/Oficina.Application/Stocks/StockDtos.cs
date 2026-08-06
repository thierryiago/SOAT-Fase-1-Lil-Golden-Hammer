namespace Oficina.Application.Stocks;

public sealed record CreateStockRequest(Guid PartId, int Quantity);

public sealed record StockMovementRequest(int Quantity);

public sealed record StockResponse(
    Guid Id,
    Guid PartId,
    int Quantity,
    DateTimeOffset CreateDate);
