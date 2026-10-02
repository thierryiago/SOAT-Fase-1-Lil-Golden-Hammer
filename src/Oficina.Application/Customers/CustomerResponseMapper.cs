using Oficina.Domain.Customers;

namespace Oficina.Application.Customers;

internal static class CustomerResponseMapper
{
    public static CustomerResponse Map(Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.TelephoneNumber,
            customer.Document);
}
