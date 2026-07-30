namespace Oficina.Application.Parts;

public sealed record CreatePartRequest(string Name, string Code, decimal UnitPrice, int StockQuantity);
