using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class CustomerRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_customer()
    {
        await using var context = CreateContext();
        var repository = new CustomerRepository(context);
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");

        await repository.AddAsync(customer, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(customer.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_customer_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new CustomerRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByDocumentAsync_should_find_customer_by_document()
    {
        await using var context = CreateContext();
        var repository = new CustomerRepository(context);
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        await repository.AddAsync(customer, CancellationToken.None);

        var result = await repository.GetByDocumentAsync("11144477735", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(customer.Id, result!.Id);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_changes()
    {
        await using var context = CreateContext();
        var repository = new CustomerRepository(context);
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        await repository.AddAsync(customer, CancellationToken.None);

        customer.Update("Ana Souza", "ana.souza@email.com", "11988880000", "11144477735");
        await repository.UpdateAsync(customer, CancellationToken.None);

        var result = await repository.GetByIdAsync(customer.Id, CancellationToken.None);
        Assert.Equal("Ana Souza", result!.Name);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-customers-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}
