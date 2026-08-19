using Microsoft.EntityFrameworkCore;
using Oficina.Application.Mechanics;
using Oficina.Domain.Mechanics;

namespace Oficina.Infrastructure.Persistence;

public sealed class MechanicRepository : IMechanicRepository
{

    private readonly AppDbContext _appDbContext;

    public MechanicRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<Mechanic>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.Mechanics.ToListAsync(cancellationToken);

    public Task<Mechanic?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return _appDbContext.Mechanics.FirstOrDefaultAsync(m => m.Id == id, cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task AddAsync(Mechanic mechanic, CancellationToken cancellationToken)
    {
        try
        {
            await _appDbContext.Mechanics.AddAsync(mechanic, cancellationToken);
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateAsync(Mechanic mechanic, CancellationToken cancellationToken)
    {
        try
        {
            _appDbContext.Mechanics.Update(mechanic);
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
