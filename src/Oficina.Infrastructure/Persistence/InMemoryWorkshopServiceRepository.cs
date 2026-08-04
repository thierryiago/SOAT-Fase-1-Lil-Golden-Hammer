using System.Collections.Concurrent;
using Oficina.Application.Services;
using Oficina.Domain.Services;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryWorkshopServiceRepository : IWorkshopServiceRepository
{
    private readonly ConcurrentDictionary<Guid, WorkshopService> _services = new();

    public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<WorkshopService>>(
            _services.Values.OrderBy(service => service.Name).ToList());

    public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _services.TryGetValue(id, out var service);
        return Task.FromResult(service);
    }

    public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var service = _services.Values.FirstOrDefault(existingService =>
            string.Equals(existingService.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(service);
    }

    public Task AddAsync(WorkshopService service, CancellationToken cancellationToken)
    {
        _services[service.Id] = service;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken)
    {
        _services[service.Id] = service;
        return Task.CompletedTask;
    }
}
