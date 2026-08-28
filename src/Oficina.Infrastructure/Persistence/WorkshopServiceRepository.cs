using Microsoft.EntityFrameworkCore;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Infrastructure.Persistence;

public sealed class WorkshopServiceRepository : IWorkshopServiceRepository
{
    private readonly AppDbContext _appDbContext;

    public WorkshopServiceRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.WorkshopServices
            .AsNoTracking()
            .OrderBy(service => service.Name)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyCollection<WorkshopService>)task.Result, cancellationToken);

    public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _appDbContext.WorkshopServices
            .FirstOrDefaultAsync(service => service.Id == id, cancellationToken);

    public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) =>
        _appDbContext.WorkshopServices
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public async Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();

        return await _appDbContext.WorkshopServices
            .AsNoTracking()
            .FirstOrDefaultAsync(service =>
                service.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);
    }

    public async Task AddAsync(WorkshopService service, CancellationToken cancellationToken)
    {
        await _appDbContext.WorkshopServices.AddAsync(service, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken)
    {
        _appDbContext.WorkshopServices.Update(service);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
