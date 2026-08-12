using Microsoft.EntityFrameworkCore;
using Oficina.Application.Customers;
using Oficina.Domain.Customers;

namespace Oficina.Infrastructure.Persistence;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _appDbContext;

    public CustomerRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) =>
        _appDbContext.Customers.ToListAsync(cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _appDbContext.Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken) =>
        _appDbContext.Customers.FirstOrDefaultAsync(c => c.Document == document, cancellationToken);


    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _appDbContext.Customers.AddAsync(customer, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        _appDbContext.Customers.Update(customer);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

}
