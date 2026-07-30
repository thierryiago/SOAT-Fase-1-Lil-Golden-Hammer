namespace Oficina.Application.Customers;

public sealed record CreateCustomerRequest(string Name, string Email, string Document);

public sealed record CustomerResponse(Guid Id, string Name, string Email, string Document);
