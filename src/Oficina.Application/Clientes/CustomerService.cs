using Oficina.Application.Common;
using Oficina.Domain.Customers;

namespace Oficina.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;

    public CustomerService(ICustomerRepository customers)
    {
        _customers = customers;
    }

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
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null || !customer.IsActive ? null : Map(customer);
    }

    public async Task<CustomerResponse> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A customer with the informed document already exists.");
        }
        if (!Customer.IsValidDocument(request.Document))
        {
            throw new ConflictException("Error validating the provided document. Verify that the CPF or CNPJ is valid.");
        }

        var customer = Customer.Create(request.Name, request.Email, request.Document);
        await _customers.AddAsync(customer, cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await GetActiveCustomerAsync(id, cancellationToken);
        var documentOwner = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (documentOwner is not null && documentOwner.Id != id)
        {
            throw new ConflictException("A customer with the informed document already exists.");
        }

        customer.Update(request.Name, request.Email, request.Document);
        await _customers.UpdateAsync(customer, cancellationToken);
        return Map(customer);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return false;
        }

        customer.Deactivate();
        await _customers.UpdateAsync(customer, cancellationToken);
        return true;
    }

    private async Task<Customer> GetActiveCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        return customer;
    }

    private static CustomerResponse Map(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Document);
}
