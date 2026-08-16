using System.ComponentModel.DataAnnotations;

namespace Oficina.Application.Stocks;

public sealed record CreateStockRequest(Guid PartId, int Quantity);

public sealed record StockMovementRequest(
    [param: Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
    int Quantity);

public sealed record StockResponse(
    Guid Id,
    Guid PartId,
    int Quantity,
    DateTimeOffset CreateDate);
