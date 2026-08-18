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
