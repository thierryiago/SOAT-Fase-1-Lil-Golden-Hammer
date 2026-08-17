using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderServiceTests
{
    [Fact]
    public async Task OpenAsync_should_return_detail_dto()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeServiceOrderRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var service = new ServiceOrderService(orders, customers, new FakePartRepository());

        ServiceOrderDetailResponse response = await service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, "Troca de oleo"), CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal("Troca de oleo", response.Description);
        Assert.Empty(response.Parts);
    }

    [Fact]
    public async Task ListAsync_should_return_summary_dtos()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeServiceOrderRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var order = ServiceOrder.Open(customer.Id, "Revisao preventiva");
        await orders.AddAsync(order, CancellationToken.None);
        var service = new ServiceOrderService(orders, customers, new FakePartRepository());

        IReadOnlyCollection<ServiceOrderListItemResponse> response = await service.ListAsync(CancellationToken.None);

        Assert.Collection(response, item => Assert.Equal(order.Id, item.Id));
    }

    [Fact]
    public async Task AddPartAsync_should_return_updated_detail_dto()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeServiceOrderRepository();
        var parts = new FakePartRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        var part = Part.Create("Filtro de oleo", "FLT-001", 35.50m, EnumPartKind.Part);
        await customers.AddAsync(customer, CancellationToken.None);
        await parts.AddAsync(part, CancellationToken.None);
        var order = ServiceOrder.Open(customer.Id, "Revisao preventiva");
        await orders.AddAsync(order, CancellationToken.None);
        var service = new ServiceOrderService(orders, customers, parts);

        ServiceOrderDetailResponse response = await service.AddPartAsync(
            order.Id, new AddPartToServiceOrderRequest(part.Id, 2), CancellationToken.None);

        Assert.Equal(71m, response.TotalParts);
        Assert.Collection(response.Parts, item =>
        {
            Assert.Equal(part.Id, item.PartId);
            Assert.Equal(2, item.QuantityUsed);
        });
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _items = new();
        public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(item => item.Document == document));
        public Task AddAsync(Customer customer, CancellationToken cancellationToken) { _items[customer.Id] = customer; return Task.CompletedTask; }
        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken) { _items[customer.Id] = customer; return Task.CompletedTask; }
    }

    private sealed class FakePartRepository : IPartRepository
    {
        private readonly Dictionary<Guid, Part> _items = new();
        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(item => item.Code == code));
        public Task AddAsync(Part part, CancellationToken cancellationToken) { _items[part.Id] = part; return Task.CompletedTask; }
        public Task UpdateAsync(Part part, CancellationToken cancellationToken) { _items[part.Id] = part; return Task.CompletedTask; }
    }

    private sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _items = new();
        public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken) { _items[serviceOrder.Id] = serviceOrder; return Task.CompletedTask; }
        public Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken) { _items[serviceOrder.Id] = serviceOrder; return Task.CompletedTask; }
    }
}
