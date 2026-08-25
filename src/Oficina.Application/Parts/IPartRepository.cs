using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

public interface IPartRepository
{
    Task<List<Part>> ListAsync(CancellationToken cancellationToken);
    Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken);
    Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Part part, CancellationToken cancellationToken);
    Task UpdateAsync(Part part, CancellationToken cancellationToken);
}
