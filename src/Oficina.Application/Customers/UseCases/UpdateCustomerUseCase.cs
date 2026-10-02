using Oficina.Application.Common;
using Oficina.Domain.Customers;

namespace Oficina.Application.Customers.UseCases;

public class UpdateCustomerUseCase(
    ICustomerRepository customers)
{
    private readonly ICustomerRepository _customers = customers;

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

        customer.Update(request.Name, request.Email, request.TelephoneNumber, request.Document);
        await _customers.UpdateAsync(customer, cancellationToken);
        return CustomerResponseMapper.Map(customer);
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
}
