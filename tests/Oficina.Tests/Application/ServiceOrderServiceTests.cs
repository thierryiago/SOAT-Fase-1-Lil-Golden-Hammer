using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Notifications;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Stock;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderServiceTests
{
    [Fact]
    public async Task TrackByDocumentAsync_should_return_empty_when_customer_not_found()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeServiceOrderRepository();
        var service = CreateService(customers, orders);

        var result = await service.TrackByDocumentAsync("11144477735", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task TrackByDocumentAsync_should_return_summaries_sorted_by_createdAt_desc()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeServiceOrderRepository();

        var customer = Customer.Create("John", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);

        var first = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "First");
        await orders.AddAsync(first, CancellationToken.None);
        await Task.Delay(10);
        var second = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Second");
        await orders.AddAsync(second, CancellationToken.None);

        var service = CreateService(customers, orders);

        var result = await service.TrackByDocumentAsync(customer.Document, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(second.Id, result.First().Id);
        Assert.Equal(first.Id, result.Last().Id);
        Assert.Equal("Second", result.First().Description);
        Assert.Equal(ServiceOrderStatus.Created.ToString(), result.First().Status);
    }

    private static NotificationService CreateNotificationService() => new(new FakeEmailSender());

    [Fact]
    public async Task TrackAsync_should_return_timeline_ordered_descending_and_map_status()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeServiceOrderRepository();
        var history = new FakeServiceOrderHistoryRepository();

        var customer = Customer.Create("Alice", "alice@email.com", "11999990000", "11144477735");
        await customers.AddAsync(customer, CancellationToken.None);

        var order = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Checkup");
        await orders.AddAsync(order, CancellationToken.None);

        var older = new ServiceOrderHistory(Guid.NewGuid(), order.Id, "Received", DateTime.UtcNow.AddMinutes(-30));
        var newer = new ServiceOrderHistory(Guid.NewGuid(), order.Id, "InDiagnosis", DateTime.UtcNow.AddMinutes(-5));
        await history.AddAsync(older, CancellationToken.None);
        await history.AddAsync(newer, CancellationToken.None);

        var service = CreateService(customers, orders, history);

        var response = await service.TrackAsync(order.Id, customer.Document, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(order.Id, response!.Id);
        Assert.Equal(2, response.History.Count);
        // timeline must be ordered by CreatedDate descending
        Assert.Equal(newer.StatusName, response.History.First().Status);
        Assert.Equal(older.StatusName, response.History.Last().Status);
    }

    private static ServiceOrderService CreateService(
        ICustomerRepository customers,
        IServiceOrderRepository orders) =>
        CreateService(customers, orders, new FakeServiceOrderHistoryRepository());

    private static ServiceOrderService CreateService(
        ICustomerRepository customers,
        IServiceOrderRepository orders,
        IServiceOrderHistoryRepository history) =>
        new(
            orders,
            customers,
            new FakeVehicleRepository(),
            new FakePartRepository(),
            new FakeWorkshopServiceRepository(),
            new FakeStockRepository(),
            history,
            new FakeBudgetService(),
            CreateNotificationService());

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
        public Task<List<ServiceOrder>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.Where(item => item.CustomerId == customerId).ToList());
        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken) { _items[serviceOrder.Id] = serviceOrder; return Task.CompletedTask; }
        public Task UpdateAsync(ServiceOrder serviceOrder, IReadOnlyCollection<ServiceOrderPart> newParts, IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices, CancellationToken cancellationToken) { _items[serviceOrder.Id] = serviceOrder; return Task.CompletedTask; }
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public Task<List<Vehicle>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(new List<Vehicle>());
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Vehicle?>(null);
        public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken) => Task.FromResult<Vehicle?>(null);
        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePartRepository : IPartRepository
    {
        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(new List<Part>());
        public Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) => Task.FromResult(new List<Part>());
        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Part?>(null);
        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult<Part?>(null);
        public Task AddAsync(Part part, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(Part part, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<WorkshopService>>(new List<WorkshopService>());
        public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) => Task.FromResult(new List<WorkshopService>());
        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<WorkshopService?>(null);
        public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult<WorkshopService?>(null);
        public Task AddAsync(WorkshopService service, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStockRepository : IStockRepository
    {
        public Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<StockPart>>(new List<StockPart>());
        public Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<StockPart?>(null);
        public Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken) => Task.FromResult<StockPart?>(null);
        public Task AddAsync(StockPart stockPart, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeServiceOrderHistoryRepository : IServiceOrderHistoryRepository
    {
        private readonly List<ServiceOrderHistory> _items = new();
        public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.ToList());
        public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) => Task.FromResult(_items.Where(h => h.OrderServiceId == serviceOrderId).ToList());
        public Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken) { _items.Add(history); return Task.CompletedTask; }
    }

    private sealed class FakeBudgetService : IBudgetService
    {
        public Task<BudgetResponse> OpenFromServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) => throw new InvalidOperationException("Budget creation was not expected in this test.");
        public Task SetApprovalByServiceOrderAsync(Guid serviceOrderId, bool isApproved, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeEmailSender : INotificationEmailSender
    {
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public int SendCount { get; private set; }

        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            SendCount++;
            return Task.CompletedTask;
        }
    }
}
