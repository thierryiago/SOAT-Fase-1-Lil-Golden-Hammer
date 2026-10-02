namespace Oficina.Application.Customers.UseCases;

public class DeleteCustomerUseCase(
    ICustomerRepository customers)
{
    private readonly ICustomerRepository _customers = customers;

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
}
