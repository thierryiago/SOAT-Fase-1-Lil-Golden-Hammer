using Oficina.Application.Customers;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderQueryUseCasesTests
{
    [Fact]
    public async Task ListServiceOrders_should_return_summary_dtos_and_propagate_cancellation()
    {
        var orders = new FakeServiceOrderRepository();
        var order = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao preventiva");
        await orders.AddAsync(order, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var useCase = new ListServiceOrdersUseCase(orders);

        var result = await useCase.ExecuteAsync(cancellation.Token);

        var response = Assert.Single(result);
        Assert.Equal(order.Id, response.Id);
        Assert.Equal(order.CustomerId, response.CustomerId);
        Assert.Equal(order.Description, response.Description);
        Assert.Equal(cancellation.Token, orders.ListCancellationToken);
    }

    [Fact]
    public async Task GetServiceOrderById_should_return_null_when_order_does_not_exist()
    {
        var useCase = new GetServiceOrderByIdUseCase(new FakeServiceOrderRepository());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceOrderById_should_return_detail_and_propagate_cancellation()
    {
        var orders = new FakeServiceOrderRepository();
        var order = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Checkup");
        await orders.AddAsync(order, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var useCase = new GetServiceOrderByIdUseCase(orders);

        var result = await useCase.ExecuteAsync(order.Id, cancellation.Token);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(cancellation.Token, orders.GetByIdCancellationToken);
    }

    [Fact]
    public async Task TrackServiceOrder_should_return_null_when_order_does_not_exist()
    {
        var useCase = new TrackServiceOrderUseCase(
            new FakeServiceOrderRepository(),
            new FakeCustomerRepository(),
            new FakeServiceOrderHistoryRepository());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            "11144477735",
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TrackServiceOrder_should_return_null_when_document_does_not_match()
    {
        var customers = new FakeCustomerRepository();
        var customer = Customer.Create("Alice", "alice@email.com", "11999990000", "11144477735");
        await customers.AddAsync(customer, CancellationToken.None);
        var orders = new FakeServiceOrderRepository();
        var order = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Checkup");
        await orders.AddAsync(order, CancellationToken.None);
        var useCase = new TrackServiceOrderUseCase(
            orders,
            customers,
            new FakeServiceOrderHistoryRepository());

        var result = await useCase.ExecuteAsync(
            order.Id,
            "52998224725",
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TrackServiceOrder_should_return_timeline_ordered_descending()
    {
        var customers = new FakeCustomerRepository();
        var customer = Customer.Create("Alice", "alice@email.com", "11999990000", "11144477735");
        await customers.AddAsync(customer, CancellationToken.None);
        var orders = new FakeServiceOrderRepository();
        var order = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Checkup");
        await orders.AddAsync(order, CancellationToken.None);
        var history = new FakeServiceOrderHistoryRepository();
        var older = new ServiceOrderHistory(Guid.NewGuid(), order.Id, "Received", DateTime.UtcNow.AddMinutes(-30));
        var newer = new ServiceOrderHistory(Guid.NewGuid(), order.Id, "InDiagnosis", DateTime.UtcNow.AddMinutes(-5));
        await history.AddAsync(older, CancellationToken.None);
        await history.AddAsync(newer, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var useCase = new TrackServiceOrderUseCase(orders, customers, history);

        var result = await useCase.ExecuteAsync(order.Id, "111.444.777-35", cancellation.Token);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(newer.StatusName, result.History.First().Status);
        Assert.Equal(older.StatusName, result.History.Last().Status);
        Assert.Equal(cancellation.Token, orders.GetByIdCancellationToken);
        Assert.Equal(cancellation.Token, customers.GetByIdCancellationToken);
        Assert.Equal(cancellation.Token, history.FindCancellationToken);
    }

    [Fact]
    public async Task TrackServiceOrdersByDocument_should_return_empty_when_customer_does_not_exist()
    {
        var useCase = new TrackServiceOrdersByDocumentUseCase(
            new FakeServiceOrderRepository(),
            new FakeCustomerRepository());

        var result = await useCase.ExecuteAsync("11144477735", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task TrackServiceOrdersByDocument_should_normalize_document_and_order_by_created_at_descending()
    {
        var customers = new FakeCustomerRepository();
        var customer = Customer.Create("John", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var orders = new FakeServiceOrderRepository();
        var first = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "First");
        await orders.AddAsync(first, CancellationToken.None);
        await Task.Delay(10);
        var second = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Second");
        await orders.AddAsync(second, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var useCase = new TrackServiceOrdersByDocumentUseCase(orders, customers);

        var result = await useCase.ExecuteAsync("529.982.247-25", cancellation.Token);

        Assert.Equal([second.Id, first.Id], result.Select(item => item.Id));
        Assert.Equal(ServiceOrderStatus.Created.ToString(), result.First().Status);
        Assert.Equal(cancellation.Token, customers.GetByDocumentCancellationToken);
        Assert.Equal(cancellation.Token, orders.ListByCustomerCancellationToken);
    }

    [Fact]
    public async Task ListSchedules_should_return_empty_when_no_orders_are_registered()
    {
        var useCase = new ListSchedulesUseCase(new FakeServiceOrderRepository());

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListSchedules_should_map_orders_and_propagate_cancellation()
    {
        var orders = new FakeServiceOrderRepository();
        var order = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Scheduled service");
        await orders.AddAsync(order, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var useCase = new ListSchedulesUseCase(orders);

        var result = await useCase.ExecuteAsync(cancellation.Token);

        var schedule = Assert.Single(result);
        Assert.Equal(order.Id, schedule.OrderServiceId);
        Assert.NotEqual(default, schedule.ScheduleDate);
        Assert.Equal(cancellation.Token, orders.ListSchedulesCancellationToken);
    }

    [Fact]
    public async Task ListSchedulesByDate_should_filter_orders_and_propagate_cancellation()
    {
        var orders = new FakeServiceOrderRepository();
        var order = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Scheduled service");
        await orders.AddAsync(order, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var useCase = new ListSchedulesByDateUseCase(orders);

        var matching = await useCase.ExecuteAsync(order.ScheduledAt, cancellation.Token);
        var notMatching = await useCase.ExecuteAsync(order.ScheduledAt.AddDays(-5), cancellation.Token);

        Assert.Equal(order.Id, Assert.Single(matching).OrderServiceId);
        Assert.Empty(notMatching);
        Assert.Equal(cancellation.Token, orders.ListSchedulesByDateCancellationToken);
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _items = [];

        public CancellationToken GetByIdCancellationToken { get; private set; }
        public CancellationToken GetByDocumentCancellationToken { get; private set; }

        public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.ToList());

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetByIdCancellationToken = cancellationToken;
            return Task.FromResult(_items.GetValueOrDefault(id));
        }

        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken)
        {
            GetByDocumentCancellationToken = cancellationToken;
            return Task.FromResult(_items.Values.FirstOrDefault(item => item.Document == document));
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            _items[customer.Id] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _items = [];

        public CancellationToken ListCancellationToken { get; private set; }
        public CancellationToken GetByIdCancellationToken { get; private set; }
        public CancellationToken ListSchedulesCancellationToken { get; private set; }
        public CancellationToken ListSchedulesByDateCancellationToken { get; private set; }
        public CancellationToken ListByCustomerCancellationToken { get; private set; }

        public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken)
        {
            ListCancellationToken = cancellationToken;
            return Task.FromResult(_items.Values.ToList());
        }

        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetByIdCancellationToken = cancellationToken;
            return Task.FromResult(_items.GetValueOrDefault(id));
        }

        public Task<List<ServiceOrder>> ListSchedulesAsync(CancellationToken cancellationToken)
        {
            ListSchedulesCancellationToken = cancellationToken;
            return Task.FromResult(_items.Values.ToList());
        }

        public Task<List<ServiceOrder>> ListSchedulesByDateAsync(
            DateTimeOffset date,
            CancellationToken cancellationToken)
        {
            ListSchedulesByDateCancellationToken = cancellationToken;
            return Task.FromResult(_items.Values.Where(item => item.ScheduledAt.Date == date.Date).ToList());
        }

        public Task<List<ServiceOrder>> ListByCustomerAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            ListByCustomerCancellationToken = cancellationToken;
            return Task.FromResult(_items.Values.Where(item => item.CustomerId == customerId).ToList());
        }

        public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
        {
            _items[serviceOrder.Id] = serviceOrder;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            ServiceOrder serviceOrder,
            IReadOnlyCollection<ServiceOrderPart> newParts,
            IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeServiceOrderHistoryRepository : IServiceOrderHistoryRepository
    {
        private readonly List<ServiceOrderHistory> _items = [];

        public CancellationToken FindCancellationToken { get; private set; }

        public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_items.ToList());

        public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(
            Guid serviceOrderId,
            CancellationToken cancellationToken)
        {
            FindCancellationToken = cancellationToken;
            return Task.FromResult(_items.Where(item => item.OrderServiceId == serviceOrderId).ToList());
        }

        public Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken)
        {
            _items.Add(history);
            return Task.CompletedTask;
        }
    }
}
