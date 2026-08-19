namespace Oficina.Application.OrderServiceHistory;

public sealed record ServiceOrderHistoryResponse(
    Guid Id,
    Guid ServiceOrderId,
    string? StatusName,
    DateTime CreatedAt);
