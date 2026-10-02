using Oficina.Application.Common;
using Oficina.Domain.Customers;

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

        var query = Customer.ListActiveMatching(customers, request.Search)
            .Select(CustomerResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}
