using Oficina.Application.Budgets;
using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;
using Oficina.Domain.OrderService;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class BudgetServiceTests
{
    [Fact]
    public async Task ListAsync_should_return_registered_budgets()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var service = CreateService(budgets, new FakeServiceOrderRepository(), new FakePartRepository(), new FakeWorkshopServiceRepository());

        var result = await service.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Collection(result.Items, item => Assert.Equal(budget.Id, item.Id));
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_budget_does_not_exist()
    {
        var service = CreateService(
            new FakeBudgetRepository(), new FakeServiceOrderRepository(), new FakePartRepository(), new FakeWorkshopServiceRepository());

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_throw_when_service_order_does_not_exist()
    {
        var service = CreateService(
            new FakeBudgetRepository(), new FakeServiceOrderRepository(), new FakePartRepository(), new FakeWorkshopServiceRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OpenFromServiceOrderAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_throw_when_service_order_has_no_workshop_services()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao");
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var service = CreateService(new FakeBudgetRepository(), serviceOrders, new FakePartRepository(), new FakeWorkshopServiceRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_throw_when_a_referenced_part_no_longer_exists()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var workshopServices = new FakeWorkshopServiceRepository();
        await workshopServices.AddAsync(workshopService, CancellationToken.None);
        var partsRepository = new FakePartRepository();
        // Note: part intentionally NOT added, simulating a part removed from the catalog.
        var service = CreateService(new FakeBudgetRepository(), serviceOrders, partsRepository, workshopServices);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_open_budget_and_calculate_total_value()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var partsRepository = new FakePartRepository();
        await partsRepository.AddAsync(part, CancellationToken.None);
        var workshopServices = new FakeWorkshopServiceRepository();
        await workshopServices.AddAsync(workshopService, CancellationToken.None);
        var budgets = new FakeBudgetRepository();
        var service = CreateService(budgets, serviceOrders, partsRepository, workshopServices);

        var response = await service.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None);

        Assert.Equal(serviceOrder.CustomerId, response.CustomerId);
        Assert.Equal(serviceOrder.Id, response.ServiceOrderId);
        Assert.Equal(130m, response.TotalValue);
        Assert.Single(response.Parts);
        Assert.Single(response.WorkshopServices);
        Assert.NotNull(await budgets.GetByIdAsync(response.Id, CancellationToken.None));
    }

    private static (ServiceOrder ServiceOrder, Part Part, WorkshopService WorkshopService) CreateServiceOrderWithItems()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao");
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);

        var serviceOrderPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, 3);
        serviceOrderPart.Part = part;
        var serviceOrderWorkshop = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id);
        serviceOrderWorkshop.WorkshopService = workshopService;

        serviceOrder.Update(
            mechanicId: null,
            description: null,
            checkList: null,
            parts: new List<ServiceOrderPart> { serviceOrderPart },
            workshopServices: new List<ServiceOrderWorkshop> { serviceOrderWorkshop });

        return (serviceOrder, part, workshopService);
    }

    private static Budget OpenBudget(ServiceOrder serviceOrder, Part part, WorkshopService workshopService)
    {
        var budgetId = Guid.NewGuid();
        var budgetPart = BudgetParts.Create(budgetId, part.Id, 3);
        budgetPart.Part = part;
        var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, workshopService.Id);
        budgetWorkshopService.WorkshopService = workshopService;

        return Budget.Open(
            budgetId,
            serviceOrder.CustomerId,
            serviceOrder.Id,
            new List<BudgetParts> { budgetPart },
            new List<BudgetWorkshopServices> { budgetWorkshopService });
    }

    private static BudgetService CreateService(
        FakeBudgetRepository budgets,
        FakeServiceOrderRepository serviceOrders,
        FakePartRepository parts,
        FakeWorkshopServiceRepository workshopServices) =>
        new(budgets, serviceOrders, parts, workshopServices);

    private sealed class FakeBudgetRepository : IBudgetRepository
    {
        private readonly Dictionary<Guid, Budget> _budgets = [];

        public Task<List<Budget>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_budgets.Values.ToList());

        public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_budgets.GetValueOrDefault(id));

        public Task AddAsync(Budget budget, CancellationToken cancellationToken)
        {
            _budgets.Add(budget.Id, budget);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _serviceOrders = [];

        public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_serviceOrders.Values.ToList());

        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_serviceOrders.GetValueOrDefault(id));

        public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
        {
            _serviceOrders.Add(serviceOrder.Id, serviceOrder);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            ServiceOrder serviceOrder,
            IReadOnlyCollection<ServiceOrderPart> newParts,
            IReadOnlyCollection<ServiceOrderWorkshop> newWorkshopServices,
            CancellationToken cancellationToken)
        {
            _serviceOrders[serviceOrder.Id] = serviceOrder;
            return Task.CompletedTask;
        }

        public Task<List<ServiceOrder>> ListSchedulesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_serviceOrders.Values.ToList());

        public Task<List<ServiceOrder>> ListSchedulesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken) =>
            Task.FromResult(_serviceOrders.Values.Where(so => so.ScheduledAt.Date == date.Date).ToList());
    }

    private sealed class FakePartRepository : IPartRepository
    {
        private readonly Dictionary<Guid, Part> _parts = [];

        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.ToList());

        public Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.Where(part => ids.Contains(part.Id)).ToList());

        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.GetValueOrDefault(id));

        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.FirstOrDefault(part =>
                string.Equals(part.Code, code.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Part part, CancellationToken cancellationToken)
        {
            _parts.Add(part.Id, part);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Part part, CancellationToken cancellationToken)
        {
            _parts[part.Id] = part;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        private readonly Dictionary<Guid, WorkshopService> _services = [];

        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<WorkshopService>>(_services.Values.ToList());

        public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult(_services.Values.Where(service => ids.Contains(service.Id)).ToList());

        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_services.GetValueOrDefault(id));

        public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult(_services.Values.FirstOrDefault(service =>
                string.Equals(service.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(WorkshopService service, CancellationToken cancellationToken)
        {
            _services.Add(service.Id, service);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken)
        {
            _services[service.Id] = service;
            return Task.CompletedTask;
        }
    }
}
