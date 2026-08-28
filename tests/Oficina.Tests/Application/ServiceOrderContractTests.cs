using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.Notifications;
using Oficina.Domain.Customers;
using Oficina.Domain.Budget;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;
using Oficina.Domain.Stock;
using Oficina.Domain.Vehicles;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderContractTests
{
    [Fact]
    public async Task OpenAsync_should_return_detail_dto()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Fiat", "Uno", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicle, CancellationToken.None);
        var service = CreateService(customers, vehicles, new FakeServiceOrderRepository());

        ServiceOrderDetailResponse response = await service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, vehicle.Id, "Troca de oleo"), CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Empty(response.Parts);
        Assert.Empty(response.WorkshopServices);
    }

    [Fact]
    public async Task ListAsync_should_return_summary_dtos()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var orders = new FakeServiceOrderRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var order = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao preventiva");
        await orders.AddAsync(order, CancellationToken.None);
        var service = CreateService(customers, vehicles, orders);

        IReadOnlyCollection<ServiceOrderListItemResponse> response = await service.ListAsync(CancellationToken.None);

        Assert.Collection(response, item => Assert.Equal(order.Id, item.Id));
    }

    [Fact]
    public async Task OpenAsync_should_throw_when_customer_does_not_exist()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var service = CreateService(customers, vehicles, new FakeServiceOrderRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync(
            new OpenServiceOrderRequest(Guid.NewGuid(), Guid.NewGuid(), "Troca de oleo"), CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_should_throw_when_vehicle_does_not_exist()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var service = CreateService(customers, vehicles, new FakeServiceOrderRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, Guid.NewGuid(), "Troca de oleo"), CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_order_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();

        var response = await context.Service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_detail_for_existing_order()
    {
        var context = await CreateOpenedOrderAsync();

        var response = await context.Service.GetByIdAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(context.ServiceOrderId, response!.Id);
    }

    [Fact]
    public async Task ApproveAsync_should_throw_when_order_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.ApproveAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CancelAsync_should_throw_when_order_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.CancelAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task FinalizeAsync_should_throw_when_order_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.FinalizeAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task DeliverAsync_should_throw_when_order_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.DeliverAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListSchedulesAsync_should_return_empty_when_no_orders_are_registered()
    {
        var service = new ServiceOrderService(
            new FakeServiceOrderRepository(),
            new FakeCustomerRepository(),
            new FakeVehicleRepository(),
            new FakePartRepository(),
            new FakeWorkshopServiceRepository(),
            new FakeStockRepository(),
            new FakeServiceOrderHistoryRepository(),
            new FakeBudgetService(),
            CreateNotificationService());

        var schedules = await service.ListSchedulesAsync();

        Assert.Empty(schedules);
    }

    [Fact]
    public async Task CancelAsync_should_skip_returning_stock_when_part_has_no_stock_record()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var workshopServices = new FakeWorkshopServiceRepository();
        var stocks = new FakeStockRepository();
        var orders = new FakeServiceOrderRepository();

        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Fiat", "Uno", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicle, CancellationToken.None);
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        await workshopServices.AddAsync(workshopService, CancellationToken.None);

        var serviceOrder = ServiceOrder.Open(customer.Id, vehicle.Id, "Revisao");
        var orphanPartId = Guid.NewGuid();
        var serviceOrderPart = ServiceOrderPart.Create(orphanPartId, serviceOrder.Id, 2);

        serviceOrder.Update(null, null, "Checklist ok", null, null);
        serviceOrder.UpdateStatus();
        serviceOrder.Update(Guid.NewGuid(), null, null, null, null);
        serviceOrder.UpdateStatus();
        serviceOrder.Update(
            null, null, null,
            new[] { serviceOrderPart },
            new[] { ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id) });
        serviceOrder.UpdateStatus();
        await orders.AddAsync(serviceOrder, CancellationToken.None);

        var service = new ServiceOrderService(
            orders, customers, vehicles, new FakePartRepository(), workshopServices, stocks,
            new FakeServiceOrderHistoryRepository(), new FakeBudgetService(), CreateNotificationService());

        var response = await service.CancelAsync(serviceOrder.Id, CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.Rejected, response.Status);
        Assert.Null(await stocks.GetByPartIdAsync(orphanPartId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_consume_stock_when_a_new_part_is_added()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);

        var response = await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Parts: [new AddPartToServiceOrderRequest(context.PartId, 5)]),
            CancellationToken.None);

        var stock = await context.Stocks.GetByPartIdAsync(context.PartId, CancellationToken.None);
        Assert.Equal(5, response.Parts.Single().QuantityUsed);
        Assert.Equal(5, stock!.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_should_return_stock_when_reducing_part_quantity()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Parts: [new AddPartToServiceOrderRequest(context.PartId, 5)]),
            CancellationToken.None);

        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Parts: [new AddPartToServiceOrderRequest(context.PartId, 2)]),
            CancellationToken.None);

        var stock = await context.Stocks.GetByPartIdAsync(context.PartId, CancellationToken.None);
        Assert.Equal(8, stock!.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_stock_is_insufficient()
    {
        var context = await CreateOpenedOrderAsync(initialStock: 3);
        await AdvanceToInDiagnosisAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Parts: [new AddPartToServiceOrderRequest(context.PartId, 10)]),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_part_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Parts: [new AddPartToServiceOrderRequest(Guid.NewGuid(), 1)]),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_workshop_service_does_not_exist()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, WorkshopServiceIds: [Guid.NewGuid()]),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_changing_mechanic_after_diagnosis_started()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, MechanicId: Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAsync_should_throw_when_order_is_not_awaiting_approval()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.ApproveAsync(context.ServiceOrderId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_create_one_budget_with_order_parts_and_services_when_awaiting_approval()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(
                context.ServiceOrderId,
                Parts: [new AddPartToServiceOrderRequest(context.PartId, 2)]),
            CancellationToken.None);

        var response = await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(
                context.ServiceOrderId,
                WorkshopServiceIds: [context.WorkshopServiceId]),
            CancellationToken.None);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Description: "Diagnostico concluido"),
            CancellationToken.None);

        var budget = await context.Budgets.GetByServiceOrderIdAsync(
            context.ServiceOrderId,
            CancellationToken.None);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, response.Status);
        Assert.NotNull(budget);
        Assert.Equal(120m, budget!.TotalValue);
        Assert.Collection(
            budget.Parts,
            part =>
            {
                Assert.Equal(context.PartId, part.PartId);
                Assert.Equal("Filtro", part.PartName);
                Assert.Equal(10m, part.UnitPrice);
                Assert.Equal(2, part.Quantity);
            });
        Assert.Collection(
            budget.WorkshopServices,
            service =>
            {
                Assert.Equal(context.WorkshopServiceId, service.WorkshopServiceId);
                Assert.Equal("Troca de oleo", service.WorkshopServiceName);
                Assert.Equal(100m, service.UnitPrice);
            });
        Assert.Single(await context.Budgets.ListAsync(CancellationToken.None));
        Assert.Equal("john@email.com", context.EmailSender.Recipient);
        Assert.Equal("John Customer - Budget Awaiting to Approval", context.EmailSender.Subject);
        Assert.Contains($"Budget ID: {budget.Id}", context.EmailSender.Body);
        Assert.Contains("Total Value: 120.00", context.EmailSender.Body);
        Assert.Equal(1, context.EmailSender.SendCount);
    }

    [Fact]
    public async Task ApproveAsync_should_advance_status_to_InExecution()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToAwaitingApprovalAsync(context);

        var response = await context.Service.ApproveAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.InExecution, response.Status);
    }

    [Fact]
    public async Task CancelAsync_should_throw_when_order_is_not_awaiting_approval()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.CancelAsync(context.ServiceOrderId, CancellationToken.None));
    }

    [Fact]
    public async Task CancelAsync_should_reject_order_and_return_consumed_parts_to_stock()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToInDiagnosisAsync(context);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, Parts: [new AddPartToServiceOrderRequest(context.PartId, 5)]),
            CancellationToken.None);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, WorkshopServiceIds: [context.WorkshopServiceId]),
            CancellationToken.None);

        var response = await context.Service.CancelAsync(context.ServiceOrderId, CancellationToken.None);

        var stock = await context.Stocks.GetByPartIdAsync(context.PartId, CancellationToken.None);
        Assert.Equal(ServiceOrderStatus.Rejected, response.Status);
        Assert.Equal(10, stock!.Quantity);
    }

    [Fact]
    public async Task FinalizeAsync_should_throw_when_order_is_not_in_execution()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToAwaitingApprovalAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.FinalizeAsync(context.ServiceOrderId, CancellationToken.None));
    }

    [Fact]
    public async Task FinalizeAsync_should_advance_status_to_Finalized()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToAwaitingApprovalAsync(context);
        await context.Service.ApproveAsync(context.ServiceOrderId, CancellationToken.None);

        var response = await context.Service.FinalizeAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.Finalized, response.Status);
    }

    [Fact]
    public async Task DeliverAsync_should_throw_when_order_is_not_finalized()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToAwaitingApprovalAsync(context);
        await context.Service.ApproveAsync(context.ServiceOrderId, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.DeliverAsync(context.ServiceOrderId, CancellationToken.None));
    }

    [Fact]
    public async Task DeliverAsync_should_advance_status_to_Delivered()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToAwaitingApprovalAsync(context);
        await context.Service.ApproveAsync(context.ServiceOrderId, CancellationToken.None);
        await context.Service.FinalizeAsync(context.ServiceOrderId, CancellationToken.None);

        var response = await context.Service.DeliverAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.Delivered, response.Status);
    }

    [Fact]
    public async Task Full_lifecycle_should_record_one_history_entry_per_real_transition()
    {
        var context = await CreateOpenedOrderAsync();
        await AdvanceToAwaitingApprovalAsync(context);
        await context.Service.ApproveAsync(context.ServiceOrderId, CancellationToken.None);
        await context.Service.FinalizeAsync(context.ServiceOrderId, CancellationToken.None);
        await context.Service.DeliverAsync(context.ServiceOrderId, CancellationToken.None);

        var history = await context.History.FindByServiceOrderAsync(context.ServiceOrderId, CancellationToken.None);

        Assert.Equal(
            new[] { "Received", "InDiagnosis", "AwaitingApproval", "InExecution", "Finalized", "Delivered" },
            history.Select(item => item.StatusName));
    }

    [Fact]
    public async Task ListSchedulesAsync_should_return_registered_service_orders()
    {
        var context = await CreateOpenedOrderAsync();

        var schedules = await context.Service.ListSchedulesAsync();

        Assert.Collection(schedules, item => Assert.Equal(context.ServiceOrderId, item.OrderServiceId));
    }

    [Fact]
    public async Task ListSchedulesByDateAsync_should_filter_out_orders_from_other_dates()
    {
        var context = await CreateOpenedOrderAsync();

        var matching = await context.Service.ListSchedulesByDateAsync(DateTime.UtcNow);
        var notMatching = await context.Service.ListSchedulesByDateAsync(DateTime.UtcNow.AddDays(-5));

        Assert.Single(matching);
        Assert.Empty(notMatching);
    }

    private static ServiceOrderService CreateService(
        ICustomerRepository customers,
        IVehicleRepository vehicles,
        IServiceOrderRepository orders) =>
        new(
            orders,
            customers,
            vehicles,
            new FakePartRepository(),
            new FakeWorkshopServiceRepository(),
            new FakeStockRepository(),
            new FakeServiceOrderHistoryRepository(),
            new FakeBudgetService(),
            CreateNotificationService());

    private sealed record ServiceOrderTestContext(
        ServiceOrderService Service,
        FakeStockRepository Stocks,
        FakeServiceOrderHistoryRepository History,
        FakeBudgetRepository Budgets,
        FakeEmailSender EmailSender,
        Guid ServiceOrderId,
        Guid PartId,
        Guid WorkshopServiceId);

    private static async Task<ServiceOrderTestContext> CreateOpenedOrderAsync(int initialStock = 10)
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var parts = new FakePartRepository();
        var workshopServices = new FakeWorkshopServiceRepository();
        var stocks = new FakeStockRepository();
        var history = new FakeServiceOrderHistoryRepository();
        var orders = new FakeServiceOrderRepository();
        var budgets = new FakeBudgetRepository();
        var emailSender = new FakeEmailSender();

        var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
        await customers.AddAsync(customer, CancellationToken.None);
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Fiat", "Uno", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicle, CancellationToken.None);

        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        await parts.AddAsync(part, CancellationToken.None);
        await stocks.AddAsync(StockPart.Create(part.Id, initialStock), CancellationToken.None);

        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        await workshopServices.AddAsync(workshopService, CancellationToken.None);

        var budgetService = new BudgetService(budgets, orders, parts, workshopServices);
        var service = new ServiceOrderService(
            orders,
            customers,
            vehicles,
            parts,
            workshopServices,
            stocks,
            history,
            budgetService,
            new NotificationService(emailSender));

        var opened = await service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, vehicle.Id, "Revisao"), CancellationToken.None);

        return new ServiceOrderTestContext(
            service,
            stocks,
            history,
            budgets,
            emailSender,
            opened.Id,
            part.Id,
            workshopService.Id);
    }

    private static async Task AdvanceToInDiagnosisAsync(ServiceOrderTestContext context)
    {
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, CheckList: "Checklist ok"), CancellationToken.None);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, MechanicId: Guid.NewGuid()), CancellationToken.None);
    }

    private static async Task AdvanceToAwaitingApprovalAsync(ServiceOrderTestContext context)
    {
        await AdvanceToInDiagnosisAsync(context);
        await context.Service.UpdateAsync(
            new UpdateServiceOrderRequest(context.ServiceOrderId, WorkshopServiceIds: [context.WorkshopServiceId]),
            CancellationToken.None);
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

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        private readonly Dictionary<Guid, Vehicle> _items = new();
        public Task<List<Vehicle>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(item => item.Plate == plate));
        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken) { _items[vehicle.Id] = vehicle; return Task.CompletedTask; }
        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken) { _items[vehicle.Id] = vehicle; return Task.CompletedTask; }
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
        private readonly Dictionary<Guid, Part> _items = new();
        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) => Task.FromResult(_items.Values.Where(part => ids.Contains(part.Id)).ToList());
        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(part => string.Equals(part.Code, code.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task AddAsync(Part part, CancellationToken cancellationToken) { _items[part.Id] = part; return Task.CompletedTask; }
        public Task UpdateAsync(Part part, CancellationToken cancellationToken) { _items[part.Id] = part; return Task.CompletedTask; }
    }

    private sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        private readonly Dictionary<Guid, WorkshopService> _items = new();
        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<WorkshopService>>(_items.Values.ToList());
        public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) => Task.FromResult(_items.Values.Where(service => ids.Contains(service.Id)).ToList());
        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(service => string.Equals(service.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task AddAsync(WorkshopService service, CancellationToken cancellationToken) { _items[service.Id] = service; return Task.CompletedTask; }
        public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken) { _items[service.Id] = service; return Task.CompletedTask; }
    }

    private sealed class FakeStockRepository : IStockRepository
    {
        private readonly Dictionary<Guid, StockPart> _items = new();
        public Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<StockPart>>(_items.Values.ToList());
        public Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(stock => stock.PartId == partId));
        public Task AddAsync(StockPart stockPart, CancellationToken cancellationToken) { _items[stockPart.Id] = stockPart; return Task.CompletedTask; }
        public Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken) { _items[stockPart.Id] = stockPart; return Task.CompletedTask; }
    }

    private sealed class FakeBudgetService : IBudgetService
    {
        public Task<BudgetResponse> OpenFromServiceOrderAsync(
            Guid serviceOrderId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Budget creation was not expected in this test.");
    }

    private static NotificationService CreateNotificationService() =>
        new(new FakeEmailSender());

    private sealed class FakeEmailSender : INotificationEmailSender
    {
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }
        public int SendCount { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string body,
            CancellationToken cancellationToken)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            SendCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBudgetRepository : IBudgetRepository
    {
        private readonly Dictionary<Guid, Budget> _items = [];

        public Task<List<Budget>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.ToList());

        public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<Budget?> GetByServiceOrderIdAsync(
            Guid serviceOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.SingleOrDefault(
                budget => budget.ServiceOrderId == serviceOrderId));

        public Task AddAsync(Budget budget, CancellationToken cancellationToken)
        {
            _items.Add(budget.Id, budget);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeServiceOrderHistoryRepository : IServiceOrderHistoryRepository
    {
        private readonly List<ServiceOrderHistory> _items = [];
        public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.ToList());
        public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Where(item => item.OrderServiceId == serviceOrderId).ToList());
        public Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken) { _items.Add(history); return Task.CompletedTask; }
    }
}
