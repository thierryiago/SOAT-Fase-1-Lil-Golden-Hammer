using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderContractTests
{
    [Fact]
    public async Task OpenAsync_should_return_detail_dto()
    {
        var customers = new FakeCustomerRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var service = CreateService(customers, new FakeServiceOrderRepository());

        ServiceOrderDetailResponse response = await service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, "Troca de oleo"), CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Empty(response.Parts);
        Assert.Empty(response.WorkshopServices);
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
        var service = CreateService(customers, orders);

        IReadOnlyCollection<ServiceOrderListItemResponse> response = await service.ListAsync(CancellationToken.None);

        Assert.Collection(response, item => Assert.Equal(order.Id, item.Id));
    }

    private static ServiceOrderService CreateService(ICustomerRepository customers, IServiceOrderRepository orders) =>
        new(orders, customers, new FakePartRepository(), new FakeWorkshopServiceRepository());

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _items = new();
        public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(item => item.Document == document));
        public Task AddAsync(Customer customer, CancellationToken cancellationToken) { _items[customer.Id] = customer; return Task.CompletedTask; }
        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken) { _items[customer.Id] = customer; return Task.CompletedTask; }
    }

    private sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _items = new();
        public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<List<ServiceOrder>> ListSchedulesAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<List<ServiceOrder>> ListSchedulesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.Where(item => item.ScheduledAt.Date == date.Date).ToList());
        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken) { _items[serviceOrder.Id] = serviceOrder; return Task.CompletedTask; }
        public Task UpdateAsync(ServiceOrder serviceOrder, IReadOnlyCollection<ServiceOrderPart> newParts, IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices, CancellationToken cancellationToken) { _items[serviceOrder.Id] = serviceOrder; return Task.CompletedTask; }
    }

    private sealed class FakePartRepository : IPartRepository
    {
        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(new List<Part>());
        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Part?>(null);
        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult<Part?>(null);
        public Task AddAsync(Part part, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(Part part, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<WorkshopService>>([]);
        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<WorkshopService?>(null);
        public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult<WorkshopService?>(null);
        public Task AddAsync(WorkshopService service, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
