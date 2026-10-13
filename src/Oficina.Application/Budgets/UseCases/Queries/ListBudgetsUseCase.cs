using Oficina.Application.Common;

namespace Oficina.Application.Budgets.UseCases.Queries;

public sealed class ListBudgetsUseCase(IBudgetRepository budgets)
{
    public async Task<PagedResponse<BudgetResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var registeredBudgets = await budgets.ListAsync(cancellationToken);
        var query = registeredBudgets
            .OrderByDescending(budget => budget.CreatedAt)
            .Select(BudgetResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}
