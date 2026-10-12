namespace Oficina.Application.Budgets.UseCases;

public sealed class SetBudgetApprovalByBudgetIdUseCase(IBudgetRepository budgets)
{
    public async Task<BudgetResponse> SetApprovalByBudgetIdAsync(
        Guid budgetId,
        bool isApproved,
        CancellationToken cancellationToken)
    {
        var budget = await budgets.GetByIdAsync(budgetId, cancellationToken)
            ?? throw new InvalidOperationException("Budget was not found.");

        if (budget.IsApproved.HasValue)
        {
            throw new InvalidOperationException(
                budget.IsApproved.Value
                    ? "Budget is already approved."
                    : "Budget is already rejected.");
        }

        budget.SetApproval(isApproved);
        await budgets.UpdateAsync(budget, cancellationToken);
        return BudgetResponseMapper.Map(budget);
    }
}
