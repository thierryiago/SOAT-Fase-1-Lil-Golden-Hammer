namespace Oficina.Application.Budgets;

public sealed record BudgetPartResponse(
    Guid Id,
    Guid PartId,
    string PartName,
    int Quantity,
    decimal UnitPrice);

public sealed record BudgetWorkshopServiceResponse(
    Guid Id,
    Guid WorkshopServiceId,
    string WorkshopServiceName,
    decimal UnitPrice);

public sealed record BudgetResponse(
    Guid Id,
    Guid CustomerId,
    Guid ServiceOrderId,
    DateTimeOffset CreatedAt,
    bool? IsApproved,
    decimal TotalValue,
    IReadOnlyCollection<BudgetPartResponse> Parts,
    IReadOnlyCollection<BudgetWorkshopServiceResponse> WorkshopServices);
