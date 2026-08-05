using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

public interface IPartRepository
{
    Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken cancellationToken);
    Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Part part, CancellationToken cancellationToken);
    Task UpdateAsync(Part part, CancellationToken cancellationToken);
}
