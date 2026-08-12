using Microsoft.EntityFrameworkCore;
using Oficina.Application.Parts;
using Oficina.Domain.Parts;

namespace Oficina.Infrastructure.Persistence;

public sealed class EfPartRepository : IPartRepository
{
    private readonly AppDbContext _context;

    public EfPartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken cancellationToken) =>
        await _context.Parts
            .AsNoTracking()
            .OrderBy(part => part.Name)
            .ToListAsync(cancellationToken);

    public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Parts.FirstOrDefaultAsync(part => part.Id == id, cancellationToken);

    public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _context.Parts.FirstOrDefaultAsync(
            part => part.Code == normalizedCode,
            cancellationToken);
    }

    public async Task AddAsync(Part part, CancellationToken cancellationToken)
    {
        await _context.Parts.AddAsync(part, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Part part, CancellationToken cancellationToken)
    {
        _context.Parts.Update(part);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
