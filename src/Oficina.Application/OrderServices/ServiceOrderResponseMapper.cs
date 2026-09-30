using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

internal static class ServiceOrderResponseMapper
{
    public static ServiceOrderListItemResponse MapListItem(ServiceOrder order) =>
        new(
            order.Id,
            order.CustomerId,
            order.VehicleId,
            order.MechanicId,
            order.Description,
            order.Status,
            order.CreatedAt,
            order.TotalParts);

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

    public static ServiceOrderSchedulesDto MapSchedule(ServiceOrder order) =>
        new()
        {
            OrderServiceId = order.Id,
            ScheduleDate = TimeZoneInfo.ConvertTimeFromUtc(
                order.ScheduledAt.DateTime,
                TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"))
        };
}
