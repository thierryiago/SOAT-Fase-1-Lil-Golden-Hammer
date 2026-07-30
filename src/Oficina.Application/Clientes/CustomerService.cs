using Oficina.Domain.Customers;

namespace Oficina.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;

    public CustomerService(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public Task<IReadOnlyCollection<Customer>> ListAsync(CancellationToken cancellationToken) =>
        _customers.ListAsync(cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _customers.GetByIdAsync(id, cancellationToken);

    public async Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name, request.Email, request.Document);
        await _customers.AddAsync(customer, cancellationToken);
        return customer;
    }
}
