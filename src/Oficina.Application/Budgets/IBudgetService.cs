using Oficina.Domain.Budget;

namespace Oficina.Application.Budgets;

public interface IBudgetService
{
    Task<BudgetResponse> OpenFromServiceOrderAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken);

    Task SetApprovalByServiceOrderAsync(
        Guid serviceOrderId,
        bool isApproved,
        CancellationToken cancellationToken);

    Task<Budget> SetApprovalByBudgetIdAsync(Guid budgetId, bool isApproved, CancellationToken cancellationToken);
}
