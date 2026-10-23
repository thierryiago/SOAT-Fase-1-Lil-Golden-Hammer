using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Application.Budgets;
using Oficina.Application.Budgets.UseCases;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Notifications;
using Oficina.Application.Notifications.UseCases;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrderHistories;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrderHistories;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Stocks;
using Oficina.Domain.Vehicles;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class OpenServiceOrderWithItemsUseCaseTests
{
    [Fact]
    public async Task Should_open_order_in_AwaitingApproval_with_items_and_no_mechanic_or_checklist()
    {
        var context = await TestContext.CreateAsync();

        var response = await context.UseCase.ExecuteAsync(context.ValidRequest(partQuantity: 2), CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, response.Status);
        Assert.Null(response.MechanicId);
        Assert.Null(response.CheckList);
        Assert.Equal(context.Customer.Id, response.CustomerId);
        Assert.Equal(context.Vehicle.Id, response.VehicleId);
        Assert.Equal(context.WorkshopService.Id, Assert.Single(response.WorkshopServices).WorkshopServiceId);
        var part = Assert.Single(response.Parts);
        Assert.Equal(context.Part.Id, part.PartId);
        Assert.Equal(2, part.QuantityUsed);
        Assert.Equal(20m, response.TotalParts);
        Assert.NotNull(await context.Orders.GetByIdAsync(response.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Should_record_history_for_every_status_in_order()
    {
        var context = await TestContext.CreateAsync();

        var response = await context.UseCase.ExecuteAsync(context.ValidRequest(), CancellationToken.None);

        var history = await context.History.FindByServiceOrderAsync(response.Id, CancellationToken.None);
        Assert.Equal(
            ["Created", "Received", "InDiagnosis", "AwaitingApproval"],
            history.Select(entry => entry.StatusName));
    }

    [Fact]
    public async Task Should_open_budget_and_send_exactly_one_email_with_opening_data()
    {
        var context = await TestContext.CreateAsync();

        var response = await context.UseCase.ExecuteAsync(context.ValidRequest(partQuantity: 2), CancellationToken.None);

        var budget = await context.Budgets.GetByServiceOrderIdAsync(response.Id, CancellationToken.None);
        Assert.NotNull(budget);
        var email = Assert.Single(context.EmailSender.Sent);
        Assert.Equal(response.Id, email.ServiceOrderId);
        Assert.Equal(context.Customer.Name, email.CustomerName);
        Assert.Equal(context.Customer.Document, email.CustomerDocument);
        Assert.Equal(context.Customer.Email, email.CustomerEmail);
        Assert.Equal(context.Customer.TelephoneNumber, email.CustomerTelephoneNumber);
        Assert.Equal(context.Vehicle.Plate, email.VehiclePlate);
        Assert.Equal(budget!.Id, email.Budget.Id);
        Assert.Equal(context.WorkshopService.Name, Assert.Single(email.Budget.WorkshopServices).WorkshopServiceName);
        Assert.Equal(context.Part.Name, Assert.Single(email.Budget.Parts).PartName);
    }

    [Fact]
    public async Task Should_reserve_stock_for_each_part()
    {
        var context = await TestContext.CreateAsync(initialStock: 10);

        await context.UseCase.ExecuteAsync(context.ValidRequest(partQuantity: 3), CancellationToken.None);

        var stock = await context.Stocks.GetByPartIdAsync(context.Part.Id, CancellationToken.None);
        Assert.Equal(7, stock!.Quantity);
    }

    [Fact]
    public async Task Should_open_order_with_services_only_when_no_parts_are_informed()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with { Parts = null };

        var response = await context.UseCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, response.Status);
        Assert.Empty(response.Parts);
        Assert.Single(context.EmailSender.Sent);
    }

    [Fact]
    public async Task Should_still_open_order_when_email_sending_fails()
    {
        var context = await TestContext.CreateAsync();
        context.EmailSender.Failure = new InvalidOperationException("SMTP unavailable.");

        var response = await context.UseCase.ExecuteAsync(context.ValidRequest(), CancellationToken.None);

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, response.Status);
    }

    [Fact]
    public async Task Should_reuse_existing_customer_and_vehicle_without_registering_new_ones()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Customer = new OpenServiceOrderWithItemsCustomerRequest("529.982.247-25", "Ignored Name"),
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest("abc-1234")
        };

        var response = await context.UseCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(context.Customer.Id, response.CustomerId);
        Assert.Equal(context.Vehicle.Id, response.VehicleId);
        Assert.Equal(0, context.Customers.AddCount);
        Assert.Equal(0, context.Vehicles.AddCount);
        Assert.Equal("John Customer", Assert.Single(context.EmailSender.Sent).CustomerName);
    }

    [Fact]
    public async Task Should_register_customer_and_vehicle_when_they_do_not_exist()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Customer = new OpenServiceOrderWithItemsCustomerRequest(
                "111.444.777-35", "New Customer", "New@Email.com", "11977777777"),
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest(
                "XYZ9876", "Ford", "Ka", 2019, EnumVehicleCategory.Car)
        };

        var response = await context.UseCase.ExecuteAsync(request, CancellationToken.None);

        var customer = await context.Customers.GetByDocumentAsync("11144477735", CancellationToken.None);
        Assert.NotNull(customer);
        Assert.Equal("New Customer", customer!.Name);
        Assert.Equal("new@email.com", customer.Email);
        var vehicle = await context.Vehicles.GetByPlateAsync("XYZ-9876", CancellationToken.None);
        Assert.NotNull(vehicle);
        Assert.Equal(customer.Id, vehicle!.CustomerId);
        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal(vehicle.Id, response.VehicleId);
        Assert.Equal(1, context.Customers.AddCount);
        Assert.Equal(1, context.Vehicles.AddCount);
        var email = Assert.Single(context.EmailSender.Sent);
        Assert.Equal("new@email.com", email.CustomerEmail);
        Assert.Equal("XYZ-9876", email.VehiclePlate);
    }

    [Fact]
    public async Task Should_register_only_the_vehicle_when_customer_already_exists()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest("XYZ9876", "Ford", "Ka", 2019)
        };

        var response = await context.UseCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(context.Customer.Id, response.CustomerId);
        Assert.NotEqual(context.Vehicle.Id, response.VehicleId);
        Assert.Equal(0, context.Customers.AddCount);
        Assert.Equal(1, context.Vehicles.AddCount);
    }

    [Fact]
    public async Task Should_reject_disabled_customer()
    {
        var context = await TestContext.CreateAsync();
        context.Customer.Deactivate();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(context.ValidRequest(), CancellationToken.None));

        Assert.Equal("The customer with the informed document has been disabled.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_disabled_vehicle()
    {
        var context = await TestContext.CreateAsync();
        context.Vehicle.Deactivate();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            context.UseCase.ExecuteAsync(context.ValidRequest(), CancellationToken.None));

        Assert.Equal("The vehicle with the informed plate has been disabled.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_reject_plate_of_another_customer(bool customerIsNew)
    {
        var context = await TestContext.CreateAsync();
        var otherCustomer = Customer.Create("Other Customer", "other@email.com", "11988888888", "11144477735");
        context.Customers.Seed(otherCustomer);
        var otherVehicle = Vehicle.Create(otherCustomer.Id, "XYZ9876", "Ford", "Ka", 2019, EnumVehicleCategory.Car);
        context.Vehicles.Seed(otherVehicle);
        var request = context.ValidRequest() with { Vehicle = new OpenServiceOrderWithItemsVehicleRequest("XYZ9876") };
        if (customerIsNew)
        {
            request = request with
            {
                Customer = new OpenServiceOrderWithItemsCustomerRequest(
                    "11222333000181", "New Company", "company@email.com", "11966666666")
            };
        }

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("The vehicle with the informed plate belongs to another customer.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_new_customer_without_registration_data()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Customer = new OpenServiceOrderWithItemsCustomerRequest("11144477735")
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.StartsWith("Customer name is required.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_new_vehicle_without_registration_data()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest("XYZ9876")
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.StartsWith("Vehicle brand is required.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_not_register_customer_or_vehicle_when_a_later_validation_fails()
    {
        var context = await TestContext.CreateAsync();
        var missing = Guid.NewGuid();
        var request = context.ValidRequest() with
        {
            Customer = new OpenServiceOrderWithItemsCustomerRequest(
                "11144477735", "New Customer", "new@email.com", "11977777777"),
            Vehicle = new OpenServiceOrderWithItemsVehicleRequest("XYZ9876", "Ford", "Ka", 2019),
            WorkshopServiceIds = [missing]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal($"Workshop service '{missing}' was not found.", exception.Message);
        context.AssertNothingWasPersisted();
        Assert.Null(await context.Customers.GetByDocumentAsync("11144477735", CancellationToken.None));
        Assert.Null(await context.Vehicles.GetByPlateAsync("XYZ-9876", CancellationToken.None));
    }

    [Fact]
    public async Task Should_reject_empty_workshop_service_list()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with { WorkshopServiceIds = [] };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("At least one workshop service is required.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_repeated_workshop_service_through_the_domain()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            WorkshopServiceIds = [context.WorkshopService.Id, context.WorkshopService.Id]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("A workshop service cannot be repeated in the service order.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_missing_workshop_service()
    {
        var context = await TestContext.CreateAsync();
        var missing = Guid.NewGuid();
        var request = context.ValidRequest() with
        {
            WorkshopServiceIds = [context.WorkshopService.Id, missing]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal($"Workshop service '{missing}' was not found.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_repeated_part_through_the_domain()
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Parts =
            [
                new OpenServiceOrderWithItemsPartRequest(context.Part.Id, 1),
                new OpenServiceOrderWithItemsPartRequest(context.Part.Id, 2)
            ]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("A part cannot be repeated in the service order.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_missing_part()
    {
        var context = await TestContext.CreateAsync();
        var missingPart = Guid.NewGuid();
        var request = context.ValidRequest() with
        {
            Parts = [new OpenServiceOrderWithItemsPartRequest(missingPart, 1)]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal($"Part '{missingPart}' was not found.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Should_reject_part_with_quantity_not_greater_than_zero(int quantity)
    {
        var context = await TestContext.CreateAsync();
        var request = context.ValidRequest() with
        {
            Parts = [new OpenServiceOrderWithItemsPartRequest(context.Part.Id, quantity)]
        };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.StartsWith("Quantity must be greater than zero.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_part_without_registered_stock()
    {
        var context = await TestContext.CreateAsync();
        var partWithoutStock = Part.Create("Pastilha", "COD-002", 50m, EnumPartKind.Part);
        await context.Parts.AddAsync(partWithoutStock, CancellationToken.None);
        var request = context.ValidRequest() with
        {
            Parts = [new OpenServiceOrderWithItemsPartRequest(partWithoutStock.Id, 1)]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("There is no stock registered for part 'Pastilha'.", exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_reject_part_with_insufficient_stock_through_the_domain()
    {
        var context = await TestContext.CreateAsync(initialStock: 10);

        var exception = await Assert.ThrowsAsync<InsufficientStockException>(() =>
            context.UseCase.ExecuteAsync(context.ValidRequest(partQuantity: 11), CancellationToken.None));

        Assert.Equal(
            $"Insufficient stock for part '{context.Part.Id}'. Available: 10, requested: 11.",
            exception.Message);
        context.AssertNothingWasPersisted();
    }

    [Fact]
    public async Task Should_not_reserve_earlier_parts_when_only_the_last_part_lacks_stock()
    {
        var context = await TestContext.CreateAsync(initialStock: 10);
        var scarcePart = Part.Create("Pastilha", "COD-002", 50m, EnumPartKind.Part);
        await context.Parts.AddAsync(scarcePart, CancellationToken.None);
        await context.Stocks.AddAsync(StockPart.Create(scarcePart.Id, 1), CancellationToken.None);
        var request = context.ValidRequest() with
        {
            Parts =
            [
                new OpenServiceOrderWithItemsPartRequest(context.Part.Id, 5),
                new OpenServiceOrderWithItemsPartRequest(scarcePart.Id, 2)
            ]
        };

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            context.UseCase.ExecuteAsync(request, CancellationToken.None));

        var stock = await context.Stocks.GetByPartIdAsync(context.Part.Id, CancellationToken.None);
        Assert.Equal(10, stock!.Quantity);
        context.AssertNothingWasPersisted();
    }

    private sealed class TestContext
    {
        public required OpenServiceOrderWithItemsUseCase UseCase { get; init; }
        public required FakeCustomerRepository Customers { get; init; }
        public required FakeVehicleRepository Vehicles { get; init; }
        public required FakePartRepository Parts { get; init; }
        public required FakeStockRepository Stocks { get; init; }
        public required FakeServiceOrderRepository Orders { get; init; }
        public required FakeServiceOrderHistoryRepository History { get; init; }
        public required FakeBudgetRepository Budgets { get; init; }
        public required FakeOpenedEmailSender EmailSender { get; init; }
        public required Customer Customer { get; init; }
        public required Vehicle Vehicle { get; init; }
        public required Part Part { get; init; }
        public required WorkshopService WorkshopService { get; init; }

        public static async Task<TestContext> CreateAsync(int initialStock = 10)
        {
            var customers = new FakeCustomerRepository();
            var vehicles = new FakeVehicleRepository();
            var parts = new FakePartRepository();
            var workshopServices = new FakeWorkshopServiceRepository();
            var stocks = new FakeStockRepository();
            var orders = new FakeServiceOrderRepository();
            var history = new FakeServiceOrderHistoryRepository();
            var budgets = new FakeBudgetRepository();
            var emailSender = new FakeOpenedEmailSender();

            var customer = Customer.Create("John Customer", "john@email.com", "11999999999", "52998224725");
            customers.Seed(customer);
            var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Fiat", "Uno", 2020, EnumVehicleCategory.Car);
            vehicles.Seed(vehicle);
            var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
            await parts.AddAsync(part, CancellationToken.None);
            await stocks.AddAsync(StockPart.Create(part.Id, initialStock), CancellationToken.None);
            var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
            await workshopServices.AddAsync(workshopService, CancellationToken.None);
            stocks.ResetUpdateCount();

            var openBudget = new OpenBudgetFromServiceOrderUseCase(budgets, orders, parts, workshopServices, new BudgetFactory());
            var sendOpenedEmail = new SendServiceOrderOpenedEmailUseCase(
                emailSender,
                NullLogger<SendServiceOrderOpenedEmailUseCase>.Instance);
            var useCase = new OpenServiceOrderWithItemsUseCase(
                orders,
                customers,
                vehicles,
                parts,
                workshopServices,
                stocks,
                history,
                openBudget,
                sendOpenedEmail);

            return new TestContext
            {
                UseCase = useCase,
                Customers = customers,
                Vehicles = vehicles,
                Parts = parts,
                Stocks = stocks,
                Orders = orders,
                History = history,
                Budgets = budgets,
                EmailSender = emailSender,
                Customer = customer,
                Vehicle = vehicle,
                Part = part,
                WorkshopService = workshopService
            };
        }

        public OpenServiceOrderWithItemsRequest ValidRequest(int partQuantity = 1) =>
            new(
                new OpenServiceOrderWithItemsCustomerRequest(Customer.Document),
                new OpenServiceOrderWithItemsVehicleRequest(Vehicle.Plate),
                "Revisao completa",
                [WorkshopService.Id],
                [new OpenServiceOrderWithItemsPartRequest(Part.Id, partQuantity)]);

        public void AssertNothingWasPersisted()
        {
            Assert.Equal(0, Customers.AddCount);
            Assert.Equal(0, Vehicles.AddCount);
            Assert.Equal(0, Orders.Count);
            Assert.Equal(0, History.Count);
            Assert.Equal(0, Budgets.Count);
            Assert.Equal(0, Stocks.UpdateCount);
            Assert.Empty(EmailSender.Sent);
        }
    }

    private sealed class FakeOpenedEmailSender : INotificationEmailSender
    {
        public List<ServiceOrderOpenedEmail> Sent { get; } = [];
        public Exception? Failure { get; set; }

        public Task SendServiceOrderOpenedAsync(ServiceOrderOpenedEmail email, CancellationToken cancellationToken)
        {
            if (Failure is not null) throw Failure;
            Sent.Add(email);
            return Task.CompletedTask;
        }

        public Task SendServiceOrderStatusAsync(ServiceOrderStatusEmail context, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendBudgetAwaitingApprovalAsync(string customerName, string customerEmail, BudgetResponse budget,
            CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null) =>
            throw new NotSupportedException();

        public Task SendVehicleReadyForPickupAsync(string customerName, string customerEmail, string vehiclePlate,
            string vehicleBrand, string vehicleModel, int vehicleYear,
            CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _items = new();
        public int AddCount { get; private set; }
        public void Seed(Customer customer) => _items[customer.Id] = customer;
        public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(item => item.Document == document));
        public Task AddAsync(Customer customer, CancellationToken cancellationToken) { AddCount++; _items[customer.Id] = customer; return Task.CompletedTask; }
        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken) { _items[customer.Id] = customer; return Task.CompletedTask; }
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        private readonly Dictionary<Guid, Vehicle> _items = new();
        public int AddCount { get; private set; }
        public void Seed(Vehicle vehicle) => _items[vehicle.Id] = vehicle;
        public Task<List<Vehicle>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(item => item.Plate == plate));
        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken) { AddCount++; _items[vehicle.Id] = vehicle; return Task.CompletedTask; }
        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken) { _items[vehicle.Id] = vehicle; return Task.CompletedTask; }
    }

    private sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _items = new();
        public int Count => _items.Count;
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

    private sealed class FakePartRepository : IPartRepository
    {
        private readonly Dictionary<Guid, Part> _items = new();
        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) => Task.FromResult(_items.Values.Where(part => ids.Contains(part.Id)).ToList());
        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(part => part.Code == code));
        public Task AddAsync(Part part, CancellationToken cancellationToken) { _items[part.Id] = part; return Task.CompletedTask; }
        public Task UpdateAsync(Part part, CancellationToken cancellationToken) { _items[part.Id] = part; return Task.CompletedTask; }
    }

    private sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        private readonly Dictionary<Guid, WorkshopService> _items = new();
        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<WorkshopService>>(_items.Values.ToList());
        public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) => Task.FromResult(_items.Values.Where(service => ids.Contains(service.Id)).ToList());
        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(service => service.Name == name));
        public Task AddAsync(WorkshopService service, CancellationToken cancellationToken) { _items[service.Id] = service; return Task.CompletedTask; }
        public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken) { _items[service.Id] = service; return Task.CompletedTask; }
    }

    private sealed class FakeStockRepository : IStockRepository
    {
        private readonly Dictionary<Guid, StockPart> _items = new();
        public int UpdateCount { get; private set; }
        public void ResetUpdateCount() => UpdateCount = 0;
        public Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<StockPart>>(_items.Values.ToList());
        public Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken) => Task.FromResult(_items.Values.FirstOrDefault(stock => stock.PartId == partId));
        public Task AddAsync(StockPart stockPart, CancellationToken cancellationToken) { _items[stockPart.Id] = stockPart; return Task.CompletedTask; }
        public Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken) { UpdateCount++; _items[stockPart.Id] = stockPart; return Task.CompletedTask; }
    }

    private sealed class FakeServiceOrderHistoryRepository : IServiceOrderHistoryRepository
    {
        private readonly List<ServiceOrderHistory> _items = [];
        public int Count => _items.Count;
        public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.ToList());
        public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Where(entry => entry.OrderServiceId == serviceOrderId).ToList());
        public Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken) { _items.Add(history); return Task.CompletedTask; }
    }

    private sealed class FakeBudgetRepository : IBudgetRepository
    {
        private readonly Dictionary<Guid, Budget> _items = [];
        public int Count => _items.Count;
        public Task<List<Budget>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(_items.Values.ToList());
        public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<Budget?> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.FirstOrDefault(budget => budget.ServiceOrderId == serviceOrderId));
        public Task AddAsync(Budget budget, CancellationToken cancellationToken) { _items.Add(budget.Id, budget); return Task.CompletedTask; }
        public Task UpdateAsync(Budget budget, CancellationToken cancellationToken) { _items[budget.Id] = budget; return Task.CompletedTask; }
    }
}
