namespace Oficina.Application.Budgets;

public interface IBudgetService
{
    Task<BudgetResponse> OpenFromServiceOrderAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken);
}
