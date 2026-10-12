namespace Oficina.Application.Budgets.UseCases.Queries;

public sealed class GetBudgetByIdUseCase(IBudgetRepository budgets)
{
    public async Task<BudgetResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await budgets.GetByIdAsync(id, cancellationToken);
        return budget is null ? null : BudgetResponseMapper.Map(budget);
    }
}
