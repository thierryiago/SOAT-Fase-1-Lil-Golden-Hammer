using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders;

public sealed record OpenServiceOrderRequest(Guid CustomerId, string Description);

public sealed record UpdateServiceOrderRequest(
    Guid ServiceOrderId,
    Guid CustomerId,
    Guid? MechanicId,
    string? Description,
    string? CheckList,
    bool? ClientApproved,
    IReadOnlyCollection<AddPartToServiceOrderRequest>? Parts,
    IReadOnlyCollection<Guid>? WorkshopServiceIds);

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

public sealed record ServiceOrderPartResponse(
    Guid Id,
    Guid PartId,
    int QuantityUsed);

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
    IReadOnlyCollection<ServiceOrderPartResponse> Parts);
