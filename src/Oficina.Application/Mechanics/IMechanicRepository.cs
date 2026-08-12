using Oficina.Domain.Mechanics;

namespace Oficina.Application.Mechanics;

public interface IMechanicRepository
{
    Task<List<Mechanic>> ListAsync(CancellationToken cancellationToken);
    Task<Mechanic?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Mechanic mechanic, CancellationToken cancellationToken);
    Task UpdateAsync(Mechanic mechanic, CancellationToken cancellationToken);
}
