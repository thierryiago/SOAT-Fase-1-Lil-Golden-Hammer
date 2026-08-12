using Microsoft.EntityFrameworkCore;
using Oficina.Application.Parts;
using Oficina.Domain.Parts;

namespace Oficina.Infrastructure.Persistence;

public sealed class PartRepository : IPartRepository
{
    private readonly AppDbContext _appDbContext;

    public PartRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<Part>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.Parts.ToListAsync(cancellationToken);

    public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _appDbContext.Parts.FirstOrDefaultAsync(part => part.Id == id, cancellationToken);

    public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _appDbContext.Parts.FirstOrDefaultAsync(part => part.Code == code, cancellationToken);

    public async Task AddAsync(Part part, CancellationToken cancellationToken)
    {
        await _appDbContext.Parts.AddAsync(part, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Part part, CancellationToken cancellationToken)
    {
        _appDbContext.Parts.Update(part);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
