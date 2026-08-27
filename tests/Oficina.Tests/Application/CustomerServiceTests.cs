using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Domain.Customers;

namespace Oficina.Tests.Application;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task ListAsync_should_return_only_active_customers()
    {
        var repository = new FakeCustomerRepository();
        var active = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        var inactive = Customer.Create("Bruno Souza", "bruno@email.com", "11988880000", "52998224725");
        inactive.Deactivate();
        await repository.AddAsync(active, CancellationToken.None);
        await repository.AddAsync(inactive, CancellationToken.None);
        var service = new CustomerService(repository);

        var result = await service.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Collection(result.Items, item => Assert.Equal(active.Id, item.Id));
    }

    [Fact]
    public async Task ListAsync_should_filter_by_search_term()
    {
        var repository = new FakeCustomerRepository();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        await repository.AddAsync(customer, CancellationToken.None);
        var service = new CustomerService(repository);

        var result = await service.ListAsync(new PageRequest(Search: "silva"), CancellationToken.None);
        var noMatch = await service.ListAsync(new PageRequest(Search: "nao-existe"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Empty(noMatch.Items);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_for_inactive_customer()
    {
        var repository = new FakeCustomerRepository();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        customer.Deactivate();
        await repository.AddAsync(customer, CancellationToken.None);
        var service = new CustomerService(repository);

        var result = await service.GetByIdAsync(customer.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_should_add_new_customer()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);

        var response = await service.CreateAsync(
            CreateRequest("Ana Silva", "ana@email.com", "11999990000", "11144477735"),
            CancellationToken.None);

        Assert.Equal("Ana Silva", response.Name);
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public async Task CreateAsync_should_throw_conflict_when_document_belongs_to_active_customer()
    {
        var repository = new FakeCustomerRepository();
        var existing = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        await repository.AddAsync(existing, CancellationToken.None);
        var service = new CustomerService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            CreateRequest("Outro Nome", "outro@email.com", "11977770000", "11144477735"),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_should_reactivate_inactive_customer_with_same_document()
    {
        var repository = new FakeCustomerRepository();
        var existing = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        existing.Deactivate();
        await repository.AddAsync(existing, CancellationToken.None);
        var service = new CustomerService(repository);

        var response = await service.CreateAsync(
            CreateRequest("Ana Reativada", "ana.nova@email.com", "11911110000", "11144477735"),
            CancellationToken.None);

        Assert.Equal(existing.Id, response.Id);
        Assert.Equal("Ana Reativada", response.Name);
        Assert.True(existing.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_should_change_customer_data()
    {
        var repository = new FakeCustomerRepository();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        await repository.AddAsync(customer, CancellationToken.None);
        var service = new CustomerService(repository);

        var response = await service.UpdateAsync(
            customer.Id,
            new UpdateCustomerRequest("Ana Souza", "ana.souza@email.com", "11988880000", "11144477735"),
            CancellationToken.None);

        Assert.Equal("Ana Souza", response.Name);
        Assert.Equal("ana.souza@email.com", response.Email);
    }

    [Fact]
    public async Task UpdateAsync_should_throw_conflict_when_document_belongs_to_another_customer()
    {
        var repository = new FakeCustomerRepository();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        var otherCustomer = Customer.Create("Bruno Souza", "bruno@email.com", "11988880000", "52998224725");
        await repository.AddAsync(customer, CancellationToken.None);
        await repository.AddAsync(otherCustomer, CancellationToken.None);
        var service = new CustomerService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(
            customer.Id,
            new UpdateCustomerRequest("Ana Silva", "ana@email.com", "11999990000", "52998224725"),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_customer_does_not_exist()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateCustomerRequest("Ana Silva", "ana@email.com", "11999990000", "11144477735"),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_should_deactivate_existing_customer()
    {
        var repository = new FakeCustomerRepository();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        await repository.AddAsync(customer, CancellationToken.None);
        var service = new CustomerService(repository);

        var result = await service.DeleteAsync(customer.Id, CancellationToken.None);

        Assert.True(result);
        Assert.False(customer.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_should_return_false_when_customer_does_not_exist()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);

        var result = await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    private static CreateCustomerRequest CreateRequest(string name, string email, string phone, string document) =>
        new(name, email, phone) { Document = document };

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _customers = [];

        public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_customers.Values.ToList());

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_customers.GetValueOrDefault(id));

        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken) =>
            Task.FromResult(_customers.Values.FirstOrDefault(customer =>
                string.Equals(customer.Document, document, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            _customers.Add(customer.Id, customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
        {
            _customers[customer.Id] = customer;
            return Task.CompletedTask;
        }
    }
}
