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

    public Task<Budget?> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        _appDbContext.Budgets
            .Include(budget => budget.Parts).ThenInclude(part => part.Part)
            .Include(budget => budget.WorkshopServices).ThenInclude(workshopService => workshopService.WorkshopService)
            .Where(budget => budget.ServiceOrderId == serviceOrderId)
            .OrderByDescending(budget => budget.CreatedAt)
            .ThenByDescending(budget => budget.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Budget budget, CancellationToken cancellationToken)
    {
        await _appDbContext.Budgets.AddAsync(budget, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Budget budget, CancellationToken cancellationToken)
    {
        _appDbContext.Budgets.Update(budget);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
