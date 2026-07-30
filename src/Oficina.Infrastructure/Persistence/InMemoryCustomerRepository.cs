using System.Collections.Concurrent;
using Oficina.Application.Customers;
using Oficina.Domain.Customers;

namespace Oficina.Infrastructure.Persistence;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();

    public Task<IReadOnlyCollection<Customer>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Customer>>(_customers.Values.OrderBy(customer => customer.Name).ToList());

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _customers.TryGetValue(id, out var customer);
        return Task.FromResult(customer);
    }

    public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken)
    {
        var normalizedDocument = NormalizeDocument(document);
        var customer = _customers.Values.FirstOrDefault(existingCustomer =>
            NormalizeDocument(existingCustomer.Document) == normalizedDocument);

        return Task.FromResult(customer);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }

    private static string NormalizeDocument(string document) =>
        new(document.Where(char.IsDigit).ToArray());
}
