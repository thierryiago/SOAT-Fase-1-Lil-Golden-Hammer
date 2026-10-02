using Oficina.Domain.WorkshopServices;

namespace Oficina.Application.WorkshopServices;

internal static class WorkshopServiceResponseMapper
{
    public static WorkshopServiceResponse Map(WorkshopService service) =>
        new(
            service.Id,
            service.Name,
            service.Description,
            service.UnitPrice,
            service.EstimatedDurationMinutes);
}
