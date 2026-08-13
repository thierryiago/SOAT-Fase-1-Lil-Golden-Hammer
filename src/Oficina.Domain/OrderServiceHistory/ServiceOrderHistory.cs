using Oficina.Domain.ServiceOrders;

namespace Oficina.Domain.OrderServiceHistory;

public record ServiceOrderHistory(Guid Id, Guid OrderServiceId, string? StatusName, DateTime CreatedDate)
{
    public ServiceOrder? OrderService { get; set; }

    public static ServiceOrderHistory Create(Guid orderServiceId, string? statusName)
    {
        if (orderServiceId == Guid.Empty)
        {
            throw new ArgumentException("Service order id is required.", nameof(orderServiceId));
        }

        return new ServiceOrderHistory(
            Guid.NewGuid(),
            orderServiceId,
            string.IsNullOrWhiteSpace(statusName) ? "Unknown" : statusName.Trim(),
            DateTime.UtcNow);
    }
}
