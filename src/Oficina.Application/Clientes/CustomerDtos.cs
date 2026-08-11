namespace Oficina.Application.Customers;

public sealed record CreateCustomerRequest(string Name, string Email, string TelephoneNumber, string Document);

public sealed record UpdateCustomerRequest(string Name, string Email, string TelephoneNumber, string Document);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string TelephoneNumber,
    string Document
    );
