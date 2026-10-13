using Oficina.Application.Budgets;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.Budget;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Parts;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

internal static class BudgetUseCaseTestFixture
{
    // Budget timestamps are assigned by the domain clock; fix them only in test fixtures
    // so ordering and version-selection assertions do not depend on clock resolution.
    public static void SetCreatedAt(Budget budget, DateTimeOffset createdAt) =>
        typeof(Budget).GetField("<CreatedAt>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(budget, createdAt);

    public static void AssertBudgetResponse(Budget budget, BudgetResponse response)
    {
        Assert.Equal(budget.Id, response.Id);
        Assert.Equal(budget.CustomerId, response.CustomerId);
        Assert.Equal(budget.ServiceOrderId, response.ServiceOrderId);
        Assert.Equal(budget.CreatedAt, response.CreatedAt);
        Assert.Equal(budget.IsApproved, response.IsApproved);
        Assert.Equal(budget.TotalValue, response.TotalValue);
        Assert.Equal(budget.Parts.Select(part => new BudgetPartResponse(
            part.Id, part.PartId, part.PartName, part.Quantity, part.UnitPrice)), response.Parts);
        Assert.Equal(budget.WorkshopServices.Select(service => new BudgetWorkshopServiceResponse(
            service.Id, service.WorkshopServiceId, service.WorkshopServiceName, service.UnitPrice)),
            response.WorkshopServices);
    }
    public static (ServiceOrder ServiceOrder, Part Part, WorkshopService WorkshopService) CreateServiceOrderWithItems()
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

    public static Budget OpenBudget(ServiceOrder serviceOrder, Part part, WorkshopService workshopService)
    {
        var budgetId = Guid.NewGuid();
        var budgetPart = BudgetParts.Create(budgetId, part.Id, part.Name, part.UnitPrice, 3);
        budgetPart.Part = part;
        var budgetWorkshopService = BudgetWorkshopServices.Create(
            budgetId, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
        budgetWorkshopService.WorkshopService = workshopService;

        return Budget.Open(
            budgetId,
            serviceOrder.CustomerId,
            serviceOrder.Id,
            new List<BudgetParts> { budgetPart },
            new List<BudgetWorkshopServices> { budgetWorkshopService });
    }

    public sealed class FakeBudgetRepository : IBudgetRepository
    {
        private readonly Dictionary<Guid, Budget> _budgets = [];
        public List<CancellationToken> Tokens { get; } = [];
        public int UpdateCount { get; private set; }

        public Task<List<Budget>> ListAsync(CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_budgets.Values.ToList());
        }

        public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_budgets.GetValueOrDefault(id));
        }

        public Task<Budget?> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_budgets.Values
                .Where(budget => budget.ServiceOrderId == serviceOrderId)
                .OrderByDescending(budget => budget.CreatedAt)
                .ThenByDescending(budget => budget.Id)
                .FirstOrDefault());
        }

        public Task AddAsync(Budget budget, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            _budgets.Add(budget.Id, budget);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Budget budget, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            UpdateCount++;
            _budgets[budget.Id] = budget;
            return Task.CompletedTask;
        }
    }

    public sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _serviceOrders = [];
        public List<CancellationToken> Tokens { get; } = [];

        public Task<List<ServiceOrder>> ListAsync(CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_serviceOrders.Values.ToList());
        }

        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_serviceOrders.GetValueOrDefault(id));
        }

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

        public Task<List<ServiceOrder>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
            Task.FromResult(_serviceOrders.Values.Where(so => so.CustomerId == customerId).ToList());
    }

    public sealed class FakePartRepository : IPartRepository
    {
        private readonly Dictionary<Guid, Part> _parts = [];
        public List<CancellationToken> Tokens { get; } = [];

        public Task<List<Part>> ListAsync(CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_parts.Values.ToList());
        }

        public Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_parts.Values.Where(part => ids.Contains(part.Id)).ToList());
        }

        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_parts.GetValueOrDefault(id));
        }

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

    public sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        private readonly Dictionary<Guid, WorkshopService> _services = [];
        public List<CancellationToken> Tokens { get; } = [];

        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<WorkshopService>>(_services.Values.ToList());

        public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_services.Values.Where(service => ids.Contains(service.Id)).ToList());
        }

        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.FromResult(_services.GetValueOrDefault(id));
        }

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
