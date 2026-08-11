using System.Collections.Concurrent;
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

    private readonly ConcurrentDictionary<Guid, Mechanic> _mechanics = new();

    public Task<List<Mechanic>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.Mechanics.AsNoTracking().ToListAsync();

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
        }
        catch (Exception)
        {
            throw;
        }
    }

    public Task UpdateAsync(Mechanic mechanic, CancellationToken cancellationToken)
    {
        _mechanics[mechanic.Id] = mechanic;
        return Task.CompletedTask;
    }
}
