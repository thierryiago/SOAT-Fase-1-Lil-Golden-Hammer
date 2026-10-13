using Oficina.Domain.Budget;

namespace Oficina.Application.Budgets;

internal static class BudgetResponseMapper
{
    public static BudgetResponse Map(Budget budget) =>
        new(
            budget.Id,
            budget.CustomerId,
            budget.ServiceOrderId,
            budget.CreatedAt,
            budget.IsApproved,
            budget.TotalValue,
            budget.Parts
                .Select(part => new BudgetPartResponse(
                    part.Id,
                    part.PartId,
                    part.PartName,
                    part.Quantity,
                    part.UnitPrice))
                .ToList(),
            budget.WorkshopServices
                .Select(service => new BudgetWorkshopServiceResponse(
                    service.Id,
                    service.WorkshopServiceId,
                    service.WorkshopServiceName,
                    service.UnitPrice))
                .ToList());
}
