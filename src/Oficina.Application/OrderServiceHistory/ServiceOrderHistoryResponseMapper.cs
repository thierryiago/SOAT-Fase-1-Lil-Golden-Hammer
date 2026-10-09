using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Application.OrderServiceHistory;

internal static class ServiceOrderHistoryResponseMapper
{
    public static ServiceOrderHistoryResponse Map(ServiceOrderHistory history) =>
        new(history.Id, history.OrderServiceId, history.StatusName, history.CreatedDate);
}
