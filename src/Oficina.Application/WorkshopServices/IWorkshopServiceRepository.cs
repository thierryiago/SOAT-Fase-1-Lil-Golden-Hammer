using Oficina.Domain.Services;

namespace Oficina.Application.WorkshopServices;

public interface IWorkshopServiceRepository
{
    Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken);
    Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(WorkshopService service, CancellationToken cancellationToken);
    Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken);
}
