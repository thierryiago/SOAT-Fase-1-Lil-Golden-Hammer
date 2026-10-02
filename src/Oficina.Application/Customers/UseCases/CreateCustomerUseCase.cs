using Oficina.Application.Common;
using Oficina.Domain.Customers;

namespace Oficina.Application.Customers.UseCases;

public class CreateCustomerUseCase(
    ICustomerRepository customers)
{
    private readonly ICustomerRepository _customers = customers;

    public async Task<CustomerResponse> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (existing is not null && !existing.IsActive)
        {
            existing.Activate();
            existing.Update(request.Name, request.Email, request.TelephoneNumber, request.Document);
            await _customers.UpdateAsync(existing, cancellationToken);
            return CustomerResponseMapper.Map(existing);
        }
        if (existing is not null)
        {
            throw new ConflictException("A customer with the informed document already exists.");
        }

        var customer = Customer.Create(request.Name, request.Email, request.TelephoneNumber, request.Document);
        await _customers.AddAsync(customer, cancellationToken);
        return CustomerResponseMapper.Map(customer);
    }
}
