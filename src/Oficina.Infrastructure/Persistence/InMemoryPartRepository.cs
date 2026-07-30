using System.Collections.Concurrent;
using Oficina.Application.Parts;
using Oficina.Domain.Parts;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryPartRepository : IPartRepository
{
    private readonly ConcurrentDictionary<Guid, Part> _parts = new();

    public Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Part>>(_parts.Values.OrderBy(part => part.Name).ToList());

    public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _parts.TryGetValue(id, out var part);
        return Task.FromResult(part);
    }

    public Task AddAsync(Part part, CancellationToken cancellationToken)
    {
        _parts[part.Id] = part;
        return Task.CompletedTask;
    }
}
