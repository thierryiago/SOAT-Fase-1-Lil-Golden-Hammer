using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public sealed record OpenServiceOrderRequest(
    Guid CustomerId,
    Guid VehicleId,
    string Description);

public sealed record UpdateServiceOrderRequest(
    Guid ServiceOrderId,
    Guid? MechanicId = null,
    string? Description = null,
    string? CheckList = null,
    IReadOnlyCollection<AddPartToServiceOrderRequest>? Parts = null,
    IReadOnlyCollection<Guid>? WorkshopServiceIds = null);

public sealed record AddPartToServiceOrderRequest(Guid PartId, int Quantity);

public sealed record ServiceOrderListItemResponse(
    Guid Id,
    Guid CustomerId,
    Guid? VehicleId,
    Guid? MechanicId,
    string Description,
    ServiceOrderStatus? Status,
    DateTimeOffset CreatedAt,
    decimal TotalParts);

public sealed record ServiceOrderPartResponse(Guid Id, Guid PartId, int QuantityUsed);

public sealed record ServiceOrderWorkshopResponse(Guid Id, Guid WorkshopServiceId);

public sealed record ServiceOrderDetailResponse(
    Guid Id,
    Guid CustomerId,
    Guid? VehicleId,
    Guid? MechanicId,
    string Description,
    string? CheckList,
    ServiceOrderStatus? Status,
    DateTimeOffset CreatedAt,
    decimal TotalParts,
    IReadOnlyCollection<ServiceOrderPartResponse> Parts,
    IReadOnlyCollection<ServiceOrderWorkshopResponse> WorkshopServices);

public sealed record ServiceOrderTrackingResponse(
    Guid Id,
    ServiceOrderStatus? Status,
    string Description,
    DateTimeOffset CreatedAt);
