using Oficina.Domain.OrderService;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public interface IServiceOrderRepository
{
    Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken);
    Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken);
    Task UpdateAsync(
        ServiceOrder serviceOrder,
        IReadOnlyCollection<ServiceOrderPart> newParts,
        IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices,
        CancellationToken cancellationToken);
    Task<List<ServiceOrder>> ListSchedulesAsync(CancellationToken cancellationToken);
    Task<List<ServiceOrder>> ListSchedulesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken);
    Task<List<ServiceOrder>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}
