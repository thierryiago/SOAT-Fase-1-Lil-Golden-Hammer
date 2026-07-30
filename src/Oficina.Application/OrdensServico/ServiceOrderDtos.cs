namespace Oficina.Application.ServiceOrders;

public sealed record OpenServiceOrderRequest(Guid CustomerId, string Description);

public sealed record AddPartToServiceOrderRequest(Guid PartId, int Quantity);
