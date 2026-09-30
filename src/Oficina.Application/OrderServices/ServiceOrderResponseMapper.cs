using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

internal static class ServiceOrderResponseMapper
{
    public static ServiceOrderDetailResponse MapDetail(ServiceOrder order) =>
        new(
            order.Id,
            order.CustomerId,
            order.VehicleId,
            order.MechanicId,
            order.Description,
            order.CheckList,
            order.Status,
            order.CreatedAt,
            order.TotalParts,
            order.Parts.Select(part => new ServiceOrderPartResponse(part.Id, part.PartId, part.QuantityUsed)).ToList(),
            order.WorkshopServices.Select(service => new ServiceOrderWorkshopResponse(service.Id, service.WorkshopServiceId)).ToList());
}
