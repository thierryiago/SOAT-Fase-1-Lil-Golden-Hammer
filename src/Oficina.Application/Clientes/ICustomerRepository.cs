using Oficina.Domain.Customers;

namespace Oficina.Application.Customers;

public interface ICustomerRepository
{
    Task<IReadOnlyCollection<Customer>> ListAsync(CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken);
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken);
}
