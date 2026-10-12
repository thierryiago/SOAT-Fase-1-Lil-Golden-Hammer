using Oficina.Domain.ServiceOrderHistories;

namespace Oficina.Application.ServiceOrderHistories;

internal static class ServiceOrderHistoryResponseMapper
{
    public static ServiceOrderHistoryResponse Map(ServiceOrderHistory history) =>
        new(history.Id, history.OrderServiceId, history.StatusName, history.CreatedDate);
}
