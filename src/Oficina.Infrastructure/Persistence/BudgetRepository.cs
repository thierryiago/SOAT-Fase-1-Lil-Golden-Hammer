using Microsoft.EntityFrameworkCore;
using Oficina.Application.Budgets;
using Oficina.Domain.Budget;

namespace Oficina.Infrastructure.Persistence;

public sealed class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _appDbContext;

    public BudgetRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<Budget>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.Budgets
            .Include(budget => budget.Parts).ThenInclude(part => part.Part)
            .Include(budget => budget.WorkshopServices).ThenInclude(workshopService => workshopService.WorkshopService)
            .ToListAsync(cancellationToken);

    public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _appDbContext.Budgets
            .Include(budget => budget.Parts).ThenInclude(part => part.Part)
            .Include(budget => budget.WorkshopServices).ThenInclude(workshopService => workshopService.WorkshopService)
            .FirstOrDefaultAsync(budget => budget.Id == id, cancellationToken);

    public async Task AddAsync(Budget budget, CancellationToken cancellationToken)
    {
        await _appDbContext.Budgets.AddAsync(budget, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
