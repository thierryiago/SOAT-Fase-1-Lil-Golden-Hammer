namespace Oficina.Application.Budgets.UseCases;

public sealed class SetBudgetApprovalByServiceOrderUseCase(IBudgetRepository budgets)
{
    public async Task SetApprovalByServiceOrderAsync(
        Guid serviceOrderId,
        bool isApproved,
        CancellationToken cancellationToken)
    {
        var budget = await budgets.GetByServiceOrderIdAsync(serviceOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Budget was not found for the service order.");

        if (budget.IsApproved.HasValue)
        {
            return;
        }

        budget.SetApproval(isApproved);
        await budgets.UpdateAsync(budget, cancellationToken);
    }
}
