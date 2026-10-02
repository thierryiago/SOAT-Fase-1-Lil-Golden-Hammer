namespace Oficina.Application.Customers.UseCases.Queries;

public class GetCustomerByIdUseCase(
    ICustomerRepository customers)
{
    private readonly ICustomerRepository _customers = customers;

    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null || !customer.IsActive ? null : CustomerResponseMapper.Map(customer);
    }
}
