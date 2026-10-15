using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.Notifications;

public sealed record ServiceOrderStatusEmail(Guid ServiceOrderId, string CustomerName,
    string CustomerEmail, string VehiclePlate, string VehicleBrand, string VehicleModel,
    int VehicleYear, ServiceOrderStatus? PreviousStatus, ServiceOrderStatus Status);
