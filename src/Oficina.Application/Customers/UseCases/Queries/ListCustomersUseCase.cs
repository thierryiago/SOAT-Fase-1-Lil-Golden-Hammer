using Oficina.Application.Common;

namespace Oficina.Application.Customers.UseCases.Queries;

public class ListCustomersUseCase(
    ICustomerRepository customers)
{
    private readonly ICustomerRepository _customers = customers;

    public async Task<PagedResponse<CustomerResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var customers = await _customers.ListAsync(cancellationToken);
        var search = request.Search?.Trim();
        var query = customers
            .Where(customer => customer.IsActive)
            .Where(customer =>
                string.IsNullOrWhiteSpace(search) ||
                customer.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                customer.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                customer.Document.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(customer => customer.Name)
            .Select(CustomerResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}
