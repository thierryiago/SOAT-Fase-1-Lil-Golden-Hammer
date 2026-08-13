using Oficina.Domain.Services;
using Oficina.Domain.Stock;

namespace Oficina.Application.ServiceOrders;

public sealed record OpenServiceOrderRequest(Guid CustomerId, string Description);

public sealed record UpdateServiceOrderRequest(
    Guid ServiceOrderId,
    Guid CustomerId,
    Guid? MechanicId,
    string? Description,
    string? CheckList,
    bool? ClientApproved,
    IReadOnlyCollection<StockParts>? Parts,
    IReadOnlyCollection<WorkshopService>? WorkshopServices);


public sealed record AddPartToServiceOrderRequest(Guid PartId, int Quantity);
