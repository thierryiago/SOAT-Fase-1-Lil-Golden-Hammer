using Oficina.Domain.Vehicles;

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

public sealed record OpenServiceOrderWithItemsRequest(
    OpenServiceOrderWithItemsCustomerRequest Customer,
    OpenServiceOrderWithItemsVehicleRequest Vehicle,
    string Description,
    IReadOnlyCollection<Guid> WorkshopServiceIds,
    IReadOnlyCollection<OpenServiceOrderWithItemsPartRequest>? Parts = null);

public sealed record OpenServiceOrderWithItemsCustomerRequest(
    string Document,
    string? Name = null,
    string? Email = null,
    string? TelephoneNumber = null);

public sealed record OpenServiceOrderWithItemsVehicleRequest(
    string Plate,
    string? Brand = null,
    string? Model = null,
    int? Year = null,
    EnumVehicleCategory Category = EnumVehicleCategory.Car);

public sealed record OpenServiceOrderWithItemsPartRequest(Guid PartId, int Quantity);

public sealed record ServiceOrderListItemResponse(
    Guid Id,
    Guid CustomerId,
    Guid? VehicleId,
    Guid? MechanicId,
    string Description,
    string? Status,
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
    string? Status,
    DateTimeOffset CreatedAt,
    decimal TotalParts,
    IReadOnlyCollection<ServiceOrderPartResponse> Parts,
    IReadOnlyCollection<ServiceOrderWorkshopResponse> WorkshopServices);

public sealed record ServiceOrderTrackingResponse(
    Guid Id,
    string? Status,
    string Description,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<ServiceOrderTrackingHistoryItem> History);

public sealed record ServiceOrderTrackingHistoryItem(string? Status, DateTime OccurredAt);

public sealed record ServiceOrderTrackingSummaryResponse(
    Guid Id,
    string? Status,
    string Description,
    DateTimeOffset CreatedAt);
