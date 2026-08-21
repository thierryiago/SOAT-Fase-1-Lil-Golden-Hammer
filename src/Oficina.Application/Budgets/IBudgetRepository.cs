using Oficina.Domain.Budget;

namespace Oficina.Application.Budgets;

public interface IBudgetRepository
{
    Task<List<Budget>> ListAsync(CancellationToken cancellationToken);
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Budget budget, CancellationToken cancellationToken);
}
